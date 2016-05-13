﻿// <copyright file="BadgeBuilder.cs" company="Spatial Focus GmbH">
// Copyright (c) Spatial Focus GmbH. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Nabble.Core.Builder
{
	using System;
	using System.Threading.Tasks;
	using Microsoft.Data.Entity.Storage;
	using Nabble.Core.Common;
	using Nabble.Core.Exceptions;

	/// <summary>
	/// Provides an implementation of <see cref="IBadgeBuilder" /> to build Badges.
	/// </summary>
	public class BadgeBuilder : IBadgeBuilder
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BadgeBuilder" /> class.
		/// </summary>
		/// <param name="badgeClient">The <see cref="IBadgeClient" /> used to request Badges.</param>
		/// <param name="statisticsService">The <see cref="IStatisticsService" /> used to modify and get certain badge statistics.</param>
		public BadgeBuilder(IBadgeClient badgeClient, IStatisticsService statisticsService)
		{
			BadgeClient = badgeClient;
			StatisticsService = statisticsService;
		}

		/// <inheritdoc />
		public event EventHandler<OnBuildBadgeErrorEventHandlerArgs> OnBuildBadgeError = (sender, args) => { };

		/// <summary>
		/// Gets or sets the BadgeClient used to request Badges.
		/// </summary>
		public IBadgeClient BadgeClient { get; set; }

		/// <summary>
		/// Gets or sets the StatisticsService used to count badge creations and requests.
		/// </summary>
		public IStatisticsService StatisticsService { get; set; }

		/// <inheritdoc />
		public async Task<Badge> BuildBadgeAsync(BadgeBuilderProperties badgeBuilderProperties,
			IAnalyzerResultAccessor analyzerResultAccessor)
		{
			string label = badgeBuilderProperties.Label;
			BadgeStyle badgeStyle = badgeBuilderProperties.Style;
			string format = badgeBuilderProperties.Format;

			try
			{
				using (IRelationalTransaction transaction = await StatisticsService.BeginTransactionAsync())
				{
					BadgeClientProperties badgeClientProperties = new BadgeClientProperties()
					{
						Label = label,
						Style = badgeStyle,
						Format = format
					};

					AnalyzerResult analyzerResult = await analyzerResultAccessor.GetAnalyzerResultAsync();

					badgeClientProperties.Color = DetermineColor(badgeBuilderProperties, analyzerResult);

					string template = DetermineTemplate(badgeBuilderProperties, analyzerResult);
					int violations = DetermineViolations(badgeBuilderProperties, analyzerResult);

					badgeClientProperties.Status = violations > 0 ? string.Format(template, violations) : template;

					Badge badge = await BadgeClient.RequestBadgeAsync(badgeClientProperties);

					await StatisticsService.AddRequestEntryAsync();

					transaction.Commit();

					return badge;
				}
			}
			catch (Exception exception)
			{
				OnBuildBadgeError(this, new OnBuildBadgeErrorEventHandlerArgs() { RaisedException = exception });

				string status = exception is BuildPendingException
					? badgeBuilderProperties.StatusTemplatePending : badgeBuilderProperties.StatusTemplateInaccessible;

				return
					await
						BadgeClient.RequestBadgeAsync(
							new BadgeClientProperties()
							{
								Label = label,
								Status = status,
								Style = badgeStyle,
								Color = badgeBuilderProperties.ColorInaccessible,
								Format = format
							});
			}
		}

		private static BadgeColor DetermineColor(BadgeBuilderProperties badgeBuilderProperties, AnalyzerResult analyzerResult)
		{
			if (analyzerResult.NumberOfErrors > 0)
			{
				return badgeBuilderProperties.ColorError;
			}

			if (analyzerResult.NumberOfWarnings > 0)
			{
				return badgeBuilderProperties.ColorWarning;
			}

			if (analyzerResult.NumberOfInfos > 0 && badgeBuilderProperties.CountInfos)
			{
				return badgeBuilderProperties.ColorInfo;
			}

			return badgeBuilderProperties.ColorSuccess;
		}

		private static string DetermineTemplate(BadgeBuilderProperties badgeBuilderProperties, AnalyzerResult analyzerResult)
		{
			if (analyzerResult.NumberOfErrors > 0)
			{
				return badgeBuilderProperties.AggregateValues
					? badgeBuilderProperties.StatusTemplateAggregate : badgeBuilderProperties.StatusTemplateError;
			}

			if (analyzerResult.NumberOfWarnings > 0)
			{
				return badgeBuilderProperties.AggregateValues
					? badgeBuilderProperties.StatusTemplateAggregate : badgeBuilderProperties.StatusTemplateWarning;
			}

			if (analyzerResult.NumberOfInfos > 0 && badgeBuilderProperties.CountInfos)
			{
				return badgeBuilderProperties.AggregateValues
					? badgeBuilderProperties.StatusTemplateAggregate : badgeBuilderProperties.StatusTemplateInfo;
			}

			return badgeBuilderProperties.StatusTemplateSuccess;
		}

		private static int DetermineViolations(BadgeBuilderProperties badgeBuilderProperties, AnalyzerResult analyzerResult)
		{
			int violations = analyzerResult.NumberOfErrors;

			if (violations == 0 || badgeBuilderProperties.AggregateValues)
			{
				violations += analyzerResult.NumberOfWarnings;
			}

			if (badgeBuilderProperties.CountInfos && (violations == 0 || badgeBuilderProperties.AggregateValues))
			{
				violations += analyzerResult.NumberOfInfos;
			}

			return violations;
		}
	}
}
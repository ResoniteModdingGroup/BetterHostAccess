using EnumerableToolkit;
using FrooxEngine;
using MonkeyLoader.Patching;
using MonkeyLoader.Resonite;
using MonkeyLoader.Resonite.DataFeeds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComponentSelectorAdditions
{
    internal sealed class ExtraHostEntryItems : DataFeedBuildingBlockMonkey<ExtraHostEntryItems, SettingsDataFeed>
    {
        // Negative because default items are added at 0
        public override int Priority => -HarmonyLib.Priority.Low;

        public override IAsyncEnumerable<DataFeedItem> Apply(IAsyncEnumerable<DataFeedItem> current, EnumerateDataFeedParameters<SettingsDataFeed> parameters)
        {
            var path = parameters.Path;

            if (path.Count != 2 || path[0] is not "Security" || path[1] is not "HostAccessSettings.Entries")
                return current;

            return ProcessAsync(current, path);
        }

        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

        private static HostAccessSettings.Entry GetEntry(string key)
            => Engine.Current.Security._settings.Entries.GetElement(key);

        private async IAsyncEnumerable<DataFeedItem> ProcessAsync(IAsyncEnumerable<DataFeedItem> current, IReadOnlyList<string> path)
        {
            await foreach (var hostGroup in current)
            {
                Logger.Debug(() => $"Processing {hostGroup.GetType().Name}: {hostGroup.ItemKey}");

                if (hostGroup is not DataFeedGroup)
                {
                    yield return hostGroup;
                    continue;
                }

                var grouping = hostGroup.SubItems[0].GroupingParameters;

                var subItems = hostGroup.SubItems.ToList();
                var insertIndex = subItems.FindIndex(item => item is DataFeedIndicator<string>);

                var hostEntry = GetEntry(hostGroup.ItemKey);

                if (hostEntry.AllowedPorts?.Count == 0 && hostEntry.BlockedPorts?.Count == 0)
                {
                    var oscMergeToggle = new DataFeedToggle();
                    oscMergeToggle.InitBase($"{hostGroup.ItemKey}.OSCMerge", path, grouping,
                        Mod.GetLocaleString("OSCMerge.Name"), Mod.GetLocaleString("OSCMerge.Description"));
                    oscMergeToggle.InitSetupValue(field => field.Value = false);

                    subItems.Insert(insertIndex, oscMergeToggle);
                }
                else
                {
                    var portsSubcategory = new DataFeedCategory();
                    portsSubcategory.InitBase($"{hostGroup.ItemKey}.Ports", path, grouping, Mod.GetLocaleString("Ports.Name"));
                    portsSubcategory.SetOverrideSubpath(hostGroup.ItemKey);

                    subItems.Insert(insertIndex, portsSubcategory);
                }

                hostGroup.SubItems = subItems;
                yield return hostGroup;
            }
        }
    }
}
namespace tomenglertde.ProjectConfigurationManager.View
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows;

    using JetBrains.Annotations;

    using TomsToolbox.Core;

    /// <summary>
    /// Interaction logic for MultipleChoiceFilter.xaml
    /// </summary>
    public sealed class MultipleChoiceTagFilter : MultipleChoiceFilterBase
    {
        [NotNull]
        private static readonly Regex Regex = new Regex(@"\W+", RegexOptions.Compiled);

        static MultipleChoiceTagFilter()
        {
            // ReSharper disable once PossibleNullReferenceException
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MultipleChoiceTagFilter), new FrameworkPropertyMetadata(typeof(MultipleChoiceFilterBase)));
        }

        protected override void OnSourceValuesChanged(IEnumerable<string> newValue)
        {
            if (newValue == null)
                Values.Clear();
            else
                // ReSharper disable once AssignNullToNotNullAttribute
                Values.SynchronizeWith(new[] { string.Empty }.Concat(newValue.SelectMany(x => Regex.Split(x))).Distinct().ToArray());
        }

        protected override MultipleChoiceContentFilterBase CreateFilter(IEnumerable<string> items)
        {
            return new TagsContentFilter(items);
        }

        private sealed class TagsContentFilter : MultipleChoiceContentFilterBase
        {
            public TagsContentFilter([CanBeNull, ItemCanBeNull] IEnumerable<string> items)
                : base(items)
            {
            }

            public override bool IsMatch(object value)
            {
                var input = value as string;
                if (string.IsNullOrWhiteSpace(input))
                    return Items?.Contains(string.Empty) ?? true;

                var tags = Regex.Split(input);

                return Items?.ContainsAny(tags) ?? true;
            }
        }
    }
}

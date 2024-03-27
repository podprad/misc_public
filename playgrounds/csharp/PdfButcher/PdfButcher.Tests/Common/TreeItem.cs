namespace PdfButcher.Tests.Common
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    [DebuggerDisplay("{" + nameof(DisplayText) + "}")]
    public class TreeItem
    {
        public static TreeItem Create(TreeItem parent, object value)
        {
            var result = new TreeItem
            {
                Parent = parent,
                Value = value,
            };

            parent?.Children.Add(result);

            return result;
        }

        public TreeItem Parent { get; set; }

        public List<TreeItem> Children { get; } = new List<TreeItem>();

        public object Value { get; set; }

        public string DisplayText => Value?.ToString() ?? string.Empty;

        public IEnumerable<TreeItem> GetLeafs()
        {
            var stack = new Stack<TreeItem>();
            stack.Push(this);

            while (stack.Any())
            {
                var current = stack.Pop();

                if (current.Children.Any())
                {
                    foreach (var child in current.Children)
                    {
                        stack.Push(child);
                    }
                }
                else
                {
                    yield return current;
                }
            }
        }
    }
}
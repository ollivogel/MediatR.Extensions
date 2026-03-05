namespace MediatR.Extensions.Shared;

using System;
using System.Collections.Generic;

internal static class GroupHierarchyHelper
{
  public static Dictionary<string, GroupNode<T>> BuildHierarchy<T>(
    IEnumerable<T> items,
    Func<T, string?> getGroupName)
  {
    var hierarchy = new Dictionary<string, GroupNode<T>>(StringComparer.Ordinal);

    foreach (var item in items)
    {
      var groupName = getGroupName(item);
      if (string.IsNullOrWhiteSpace(groupName))
      {
        continue;
      }

      var parts = groupName.Split('.', StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length == 0)
      {
        continue;
      }

      // Recursively create/find nodes and add item
      AddItemToHierarchy(hierarchy, parts, 0, item);
    }

    return hierarchy;
  }

  private static void AddItemToHierarchy<T>(
    Dictionary<string, GroupNode<T>> currentLevel,
    string[] parts,
    int index,
    T item)
  {
    var partName = parts[index];

    // Create node if not present
    if (!currentLevel.TryGetValue(partName, out var node))
    {
      node = new GroupNode<T>(
        partName,
        new Dictionary<string, GroupNode<T>>(StringComparer.Ordinal),
        []);
      currentLevel[partName] = node;
    }

    // Last segment? -> Add item
    if (index == parts.Length - 1)
    {
      node.Items.Add(item);
    }
    else
    {
      // Recurse deeper
      AddItemToHierarchy(node.Children, parts, index + 1, item);
    }
  }
}

internal sealed class GroupNode<T>
{
  public string Name { get; }
  public Dictionary<string, GroupNode<T>> Children { get; }
  public List<T> Items { get; }

  public GroupNode(string name, Dictionary<string, GroupNode<T>> children, List<T> items)
  {
    Name = name;
    Children = children;
    Items = items;
  }
}

namespace AiSetupUi;

internal enum NodeKind { Root, Category, Item, Info, ExtrasRoot }

/// <summary>What each tree node carries around in its Tag.</summary>
internal sealed class NodeTag
{
    public NodeKind Kind { get; init; }
    public string Name { get; init; } = "";
    public SetupItem? Item { get; init; }
    public bool? Installed { get; set; }
    public bool StatusKnown { get; set; }
    public bool Matched { get; set; }

    public string DisplayName =>
        Item is { IsExtra: true } && !string.IsNullOrEmpty(Item.Version) ? $"{Name} {Item.Version}" : Name;
}

/// <summary>Owns the checkbox tree: building it from the catalog, ticking, filtering, status labels and colours.</summary>
internal sealed class SetupTree
{
    private readonly TreeView _tree;
    private bool _suppressCheckEvents;

    public event Action? SelectionChanged;

    public TreeView Tree => _tree;

    public SetupTree(TreeView tree)
    {
        _tree = tree;
        _tree.CheckBoxes = true;
        _tree.AfterCheck += OnAfterCheck;
    }

    // ---------------- building ----------------

    public void Build()
    {
        _tree.BeginUpdate();

        var tools = MakeNode("Applications & CLI Tools (winget)", NodeKind.Root);
        foreach (var (category, packages) in Catalog.Winget)
        {
            var catNode = MakeNode(category, NodeKind.Category);
            foreach (var p in packages)
                catNode.Nodes.Add(MakeNode(p.Name, NodeKind.Item,
                    new SetupItem { Type = ItemType.Winget, Id = p.Id, Name = p.Name }));
            tools.Nodes.Add(catNode);
        }
        _tree.Nodes.Add(tools);

        var tweaks = MakeNode("Optional System Tweaks (modifies Windows settings - off by default)", NodeKind.Root);
        foreach (var t in Catalog.Tweaks)
            tweaks.Nodes.Add(MakeNode(t.Name, NodeKind.Item,
                new SetupItem { Type = ItemType.Tweak, Id = t.Id, Name = t.Name, Tweak = t }));
        _tree.Nodes.Add(tweaks);

        var pip = MakeNode("Python / Data Science Packages (pip, installed into your venv)", NodeKind.Root);
        foreach (var (category, packages) in Catalog.Pip)
        {
            var catNode = MakeNode(category, NodeKind.Category);
            foreach (var pkg in packages)
                catNode.Nodes.Add(MakeNode(pkg, NodeKind.Item,
                    new SetupItem { Type = ItemType.Pip, Id = pkg, Name = pkg }));
            pip.Nodes.Add(catNode);
        }
        _tree.Nodes.Add(pip);

        var project = MakeNode("Project Setup", NodeKind.Root);
        foreach (var o in Catalog.ProjectOptions)
            project.Nodes.Add(MakeNode(o.Name, NodeKind.Item,
                new SetupItem { Type = ItemType.Project, Id = o.Id, Name = o.Name }));
        _tree.Nodes.Add(project);

        _tree.ExpandAll();
        RestyleCore();
        _tree.EndUpdate();
    }

    private static TreeNode MakeNode(string text, NodeKind kind, SetupItem? item = null) =>
        new(text) { Tag = new NodeTag { Kind = kind, Name = text, Item = item } };

    // ---------------- walking & selection ----------------

    private static IEnumerable<TreeNode> Descendants(TreeNodeCollection nodes)
    {
        foreach (TreeNode n in nodes)
        {
            yield return n;
            foreach (var c in Descendants(n.Nodes)) yield return c;
        }
    }

    public IEnumerable<TreeNode> Leaves() => Descendants(_tree.Nodes).Where(n => n.Nodes.Count == 0);

    public List<SetupItem> CheckedItems() =>
        Leaves().Where(n => n.Checked && n.Tag is NodeTag { Item: not null })
                .Select(n => ((NodeTag)n.Tag).Item!)
                .ToList();

    public int CheckedCount => CheckedItems().Count;

    private void OnAfterCheck(object? sender, TreeViewEventArgs e)
    {
        if (_suppressCheckEvents || e.Node == null) return;

        _suppressCheckEvents = true;
        try
        {
            // push the state down to every child...
            foreach (var d in Descendants(e.Node.Nodes)) d.Checked = e.Node.Checked;

            // ...and keep parents ticked while any child is ticked
            for (var p = e.Node.Parent; p != null; p = p.Parent)
                p.Checked = p.Nodes.Cast<TreeNode>().Any(c => c.Checked);
        }
        finally
        {
            _suppressCheckEvents = false;
        }
        SelectionChanged?.Invoke();
    }

    /// <summary>Set the checked state for a specific node and all its descendants, and roll up its parents.</summary>
    public void SetCheckedForNode(TreeNode node, bool value)
    {
        _suppressCheckEvents = true;
        _tree.BeginUpdate();
        try
        {
            node.Checked = value;
            foreach (var d in Descendants(node.Nodes)) d.Checked = value;

            for (var p = node.Parent; p != null; p = p.Parent)
                p.Checked = p.Nodes.Cast<TreeNode>().Any(c => c.Checked);
        }
        finally
        {
            _tree.EndUpdate();
            _suppressCheckEvents = false;
        }
        SelectionChanged?.Invoke();
    }

    public void SetAllChecked(bool value)
    {
        _suppressCheckEvents = true;
        _tree.BeginUpdate();
        try
        {
            foreach (var n in Descendants(_tree.Nodes)) n.Checked = value;
        }
        finally
        {
            _tree.EndUpdate();
            _suppressCheckEvents = false;
        }
        SelectionChanged?.Invoke();
    }

    // ---------------- filter ----------------

    public void ApplyFilter(string query)
    {
        var q = query.Trim();
        _tree.BeginUpdate();
        try
        {
            if (q.Length == 0)
            {
                foreach (var n in Descendants(_tree.Nodes))
                    if (n.Tag is NodeTag t) t.Matched = false;
                _tree.ExpandAll();
            }
            else
            {
                foreach (TreeNode root in _tree.Nodes) FilterWalk(root, q);
            }
            RestyleCore();
        }
        finally
        {
            _tree.EndUpdate();
        }
    }

    private static bool FilterWalk(TreeNode node, string q)
    {
        var tag = node.Tag as NodeTag;
        var name = tag?.Name ?? node.Text;
        bool selfMatch = name.Contains(q, StringComparison.OrdinalIgnoreCase);

        bool childMatch = false;
        foreach (TreeNode c in node.Nodes)
            if (FilterWalk(c, q)) childMatch = true;

        if (tag != null) tag.Matched = selfMatch;

        if (selfMatch || childMatch)
        {
            node.Expand();
            return true;
        }

        node.Collapse();
        return false;
    }

    // ---------------- extras from "Scan System Extras" ----------------

    public void RemoveExtras()
    {
        var old = _tree.Nodes.Cast<TreeNode>()
            .Where(n => n.Tag is NodeTag { Kind: NodeKind.ExtrasRoot }).ToList();
        foreach (var n in old) _tree.Nodes.Remove(n);
    }

    public void ShowExtras(IReadOnlyList<PipPackage> extras)
    {
        var root = MakeNode($"Extra Python Packages Found on Your PC ({extras.Count} not in catalog)", NodeKind.ExtrasRoot);

        if (extras.Count == 0)
        {
            root.Nodes.Add(new TreeNode("(none - every installed package is already in the catalog)")
            {
                Tag = new NodeTag { Kind = NodeKind.Info, Name = "(none)" },
            });
        }
        else
        {
            foreach (var p in extras)
            {
                var item = new SetupItem { Type = ItemType.Pip, Id = p.Name, Name = p.Name, IsExtra = true, Version = p.Version };
                var tag = new NodeTag { Kind = NodeKind.Item, Name = p.Name, Item = item, Installed = true, StatusKnown = true };
                root.Nodes.Add(new TreeNode(LeafText(tag, true)) { Tag = tag });
            }
        }

        _tree.Nodes.Add(root);
        RestyleCore();
        root.Expand();
        root.EnsureVisible();
    }

    // ---------------- install status ----------------

    public (int Installed, int Missing, int Unknown) ApplyStatus(StatusSnapshot snapshot)
    {
        int installed = 0, missing = 0, unknown = 0;
        _tree.BeginUpdate();
        try
        {
            foreach (var leaf in Leaves())
            {
                if (leaf.Tag is not NodeTag { Item: not null } tag) continue;

                var result = snapshot.Evaluate(tag.Item);
                tag.Installed = result;
                tag.StatusKnown = true;
                leaf.Text = LeafText(tag, result);

                if (result == true) installed++;
                else if (result == false) missing++;
                else unknown++;
            }

            foreach (TreeNode root in _tree.Nodes) UpdateCategoryLabels(root);
            RestyleCore();
        }
        finally
        {
            _tree.EndUpdate();
        }
        return (installed, missing, unknown);
    }

    private static string LeafText(NodeTag tag, bool? installed) => installed switch
    {
        true => $"{tag.DisplayName}   [installed]",
        false => $"{tag.DisplayName}   [not installed]",
        _ => $"{tag.DisplayName}   [unknown]",
    };

    private static (int Total, int Installed) CountLeaves(TreeNode node)
    {
        if (node.Nodes.Count == 0)
            return node.Tag is NodeTag { Item: not null } t ? (1, t.Installed == true ? 1 : 0) : (0, 0);

        int total = 0, installed = 0;
        foreach (TreeNode c in node.Nodes)
        {
            var (t, i) = CountLeaves(c);
            total += t;
            installed += i;
        }
        return (total, installed);
    }

    private static void UpdateCategoryLabels(TreeNode node)
    {
        foreach (TreeNode c in node.Nodes) UpdateCategoryLabels(c);

        if (node.Nodes.Count > 0 && node.Tag is NodeTag { Kind: NodeKind.Root or NodeKind.Category } tag)
        {
            var (total, installed) = CountLeaves(node);
            // "◐" marks a partially-installed group so users can see mixed state at a glance.
            var indicator = installed > 0 && installed < total ? " ◐" : "";
            node.Text = $"{tag.Name}{indicator}   [{installed}/{total} installed]";
        }
    }

    // ---------------- colours ----------------

    /// <summary>Re-colours every node from the current theme. Call after a theme change.</summary>
    public void Restyle()
    {
        _tree.BeginUpdate();
        try { RestyleCore(); }
        finally { _tree.EndUpdate(); }
    }

    private void RestyleCore()
    {
        var theme = ThemeManager.Current;
        foreach (var n in Descendants(_tree.Nodes))
        {
            if (n.Tag is not NodeTag tag) continue;
            n.ForeColor = theme.NodeColor(tag.Kind, tag.Installed, tag.StatusKnown, tag.Item?.IsExtra == true);
            n.BackColor = tag.Matched ? theme.Match : Color.Empty;
        }
    }
}
using FreeSql.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Blueprint.Net.Server
{
    public class Card
    {
        [Column(IsIdentity = true, IsPrimary = true)] public long Id { get; set; }
        public long WorkspaceId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public string Label { get; set; }
        public string Type { get; set; }
        [Navigate(nameof(CardNodeInfo.CardId))] public List<CardNodeInfo> Nodes { get; set; } = [];
        public List<string> TitleBarColor { get; set; } = [];
    }

    public class Link
    {
        [Column(IsIdentity = true, IsPrimary = true)] public long Id { get; set; }
        public long WorkspaceId { get; set; }

        [Navigate(nameof(SourceId))] public NodeConnectInfo Source { get; set; }

        [Navigate(nameof(TargetId))] public NodeConnectInfo Target { get; set; }

        public long SourceId { get; set; }
        public long TargetId { get; set; }

    }

    public class NodeConnectInfo
    {
        [Column(IsIdentity = true, IsPrimary = true)] public long Id { get; set; }
        public string Node { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public string Color { get; set; }
        public string EnumType { get; set; }
    }

    public class CardNodeInfo
    {
        [Column(IsIdentity = true, IsPrimary = true)]  public long Id { get; set; }

        public long CardId { get; set; }
        public string Type { get; set; }
        public int Level { get; set; }
        public string EnumType { get; set; }
        public string Color { get; set; }
        public int? MultiConnected { get; set; }
        public string Slot { get; set; }
        public string Label { get; set; }
        public string Value { get; set; }
    }





    public class Project
    {
        [Column(IsIdentity = true, IsPrimary = true)] public long Id { get; set; }
        public string Name { get; set; }
        [Navigate(nameof(Workspace.ProjectId))] public List<Workspace> Workspaces { get; set; } = [];
    }

    public class Workspace
    {
        [Column(IsIdentity = true, IsPrimary = true)] public long Id { get; set; }
        public long ProjectId { get; set; }
        public string Name { get; set; }
        [Navigate(nameof(Card.WorkspaceId))] public List<Card> Cards { get; set; } = [];
        [Navigate(nameof(Card.WorkspaceId))] public List<Link> Links { get; set; } = [];
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AHP;
internal class Node
{
    private double _weight = 1;
    public Node? ParentNode { get; set; } 
    public required string Name { get; set; }
    public double Weight
    {
        get => _weight;
        set => _weight = value * (ParentNode?.Weight ?? 1);
    }
    public LinkedList<Node> SubNodes { get; set; } = new();
}

using AHP;
using AHP.AutoMapper;
using AutoMapper;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

var autoMapperConfig = new MapperConfiguration(config =>
{
    config.AddProfile<AutoMapperProfile>();
});
var mapper = new Mapper(autoMapperConfig);

Console.WriteLine("How many alternatives do you have (from 2 up to 3)?\n" +
    "List them, each one separated with a comma(,):");
string[]? alternatives = Console.ReadLine()?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
if (alternatives is null || alternatives.Length <= 1 || alternatives.Length > 3)
{
    Console.WriteLine("You have to enter at least 2 alternatives or up to 3.");
    return;
}

Console.WriteLine("\nHow many top-level criteria do you want? (from 1 up to 3)?\n" +
    "List them, each one separated with a comma(,):");
string[]? topLevelCriteria = Console.ReadLine()?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
if (topLevelCriteria is null || topLevelCriteria.Length == 0 || topLevelCriteria.Length > 3)
{
    Console.WriteLine("You have to enter at least 1 criterion or up to 3.");
    return;
}

Node rootNode = new() { Name = "Root" };
LinkedList<Node> nodes = mapper.Map<LinkedList<Node>>(topLevelCriteria);
rootNode.SubNodes = nodes;
LinkedList<Node> leafNodes = new();
BuildHierarchy(rootNode, nodes, leafNodes, 1, 4, mapper);

Console.WriteLine("\nHierarchy:");
DisplayNodes(nodes, 0, (node) => node.Name);

EnterJudgments(rootNode, mapper);

Console.WriteLine("\nWeights:");
DisplayNodes(nodes, 0, (node) => $"{node.Name} ( {node.Weight:F3} )");

LinkedList<Node> optionNodes = mapper.Map<LinkedList<Node>>(alternatives);
Dictionary<Node, double> optionValues = EnterAndEvaluateAlternatives(leafNodes, optionNodes, mapper);
Console.WriteLine("\nFinalized estimates of the alternatives:");
foreach (var optionValue in optionValues)
{
    Console.WriteLine($"{optionValue.Key.Name} ( {optionValue.Value} )");
}


#region Methods
static Dictionary<Node, double> EnterAndEvaluateAlternatives(LinkedList<Node> leafNodes, LinkedList<Node> optionNodes, IMapper mapper)
{
    Dictionary<Node, double> optionValues = new();
    foreach (var optionNode in optionNodes)
    {
        optionValues.Add(optionNode, 0);
    }

    foreach (Node leafNode in leafNodes)
    {
        foreach (Node optionNode in optionNodes)
        {
            optionNode.ParentNode = leafNode;
        }

        Console.WriteLine($"\nFill in the judgments of the alternatives regarding the {leafNode.Name} criterion:");
        FillJudgments(optionNodes, mapper);
        foreach (Node optionNode in optionNodes)
        {
            optionValues[optionNode] += optionNode.Weight;
        }
    }

    return optionValues;
}

static void EnterJudgments(Node rootNode, IMapper mapper)
{
    Queue<LinkedList<Node>> nodesQueue = new();
    Queue<Node> nodeQueue = new(new[] { rootNode });
    while (nodeQueue.Count > 0)
    {
        Node parentNode = nodeQueue.Dequeue();
        LinkedList<Node> subNodes = parentNode.SubNodes;
        nodesQueue.Enqueue(subNodes);
        foreach (var node in subNodes)
        {
            if (node.SubNodes.Count > 0)
            {
                nodeQueue.Enqueue(node);
            }
        }
    }

    while (nodesQueue.Count > 0)
    {
        var nodes = nodesQueue.Dequeue();
        string parentNodeName = nodes.First?.Value.ParentNode?.Name ?? throw new InvalidOperationException();
        Console.WriteLine($"\nFill in the judgments of the {parentNodeName} level:");
        FillJudgments(nodes, mapper);
    }
}

static void FillJudgments(LinkedList<Node> nodes, IMapper mapper)
{
    Repeat(() =>
    {
        string nodesGlossary = nodes.Aggregate("\t", (glossary, node) => $"{glossary}{node.Name}\t");
        Console.WriteLine($"{nodesGlossary}");
        double[][] judgments = new double[nodes.Count][];
        var listNode = nodes.First;
        for (int i = 0; i < nodes.Count; i++)
        {
            var node = listNode?.Value ?? throw new InvalidOperationException();
            Repeat(() =>
            {
                Console.Write($"{node.Name}\t");
                string[]? fractions = Console.ReadLine()?.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (fractions is null || fractions.Length < nodes.Count)
                {
                    Console.WriteLine("The number of entries ought to match that of the criteria.");
                    return false;
                }

                judgments[i] = mapper.Map<double[]>(fractions);
                return true;
            });

            listNode = listNode.Next;
        }

        double[,] normalizedJudgments = GetNormalizedJudgments(judgments);
        double[] weights = GetAndSetWeights(normalizedJudgments, nodes);
        if (GetCoherenceRatio(judgments, weights) > 0.1)
        {
            Console.WriteLine("The coherence of your reasoning is flawed. Try reconsidering the judgments.");
            return false;
        }

        return true;
    });
}

static double[] GetAndSetWeights(double[,] normalizedJudgments, LinkedList<Node> nodes)
{
    double[] weights = new double[normalizedJudgments.GetLength(0)];
    var listNode = nodes.First;
    for (int x = 0; x < weights.Length; x++)
    {
        for (int y = 0; y < weights.Length; y++)
        {
            weights[x] += normalizedJudgments[x, y];
        }

        weights[x] /= weights.Length;
        listNode!.Value.Weight = weights[x];
        listNode = listNode.Next;
    }

    return weights;
}

static double[,] GetNormalizedJudgments(double[][] judgments)
{
    int judgmentsDimension = judgments.GetLength(0);
    double[,] normalizedJudgments = new double[judgmentsDimension, judgmentsDimension];
    for (int y = 0; y < judgmentsDimension; y++)
    {
        double columnSum = judgments.SumByColumn(y);
        for (int x = 0; x < judgmentsDimension; x++)
        {
            normalizedJudgments[x, y] = judgments[x][y] / columnSum;
        }
    }

    return normalizedJudgments;
}

static double GetCoherenceRatio(double[][] judgments, double[] weights)
{
    Matrix<double> judgmentsMatrix = DenseMatrix.OfRowArrays(judgments);
    Matrix<double> weightsMatrix = DenseMatrix.OfColumnMajor(weights.Length, 1, weights);
    double nMax = (judgmentsMatrix * weightsMatrix).ColumnSums().Sum();
    int n = judgmentsMatrix.ColumnCount;
    double CI = (nMax - n) / (n - 1);
    double RI = (1.98 * (n - 2) + Math.Pow(Math.E, -8)) / n;
    return CI / RI;
}

static void BuildHierarchy(Node? parentNode, IEnumerable<Node> nodes, LinkedList<Node> leafNodes, int level, int limit, IMapper mapper)
{
    if (level == limit)
    {
        return;
    }

    foreach (var node in nodes)
    {
        node.ParentNode = parentNode;
        Repeat(() =>
        {
            Console.WriteLine($"\nDoes {node.Name} have sub-criteria? (from 2 up to 3)?\n" +
    "List them, each one separated with a comma(,); or press 'Enter' to skip them:");
            string[]? criteria = Console.ReadLine()?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (criteria is null || criteria.Length == 0)
            {
                Console.WriteLine($"{node.Name} is rendered sub-strata-less.");
                leafNodes.AddLast(node);
                return true;
            }
            if (criteria.Length <= 1 || criteria.Length > 3)
            {
                Console.WriteLine("A criterion ought to have at least 2 sub-criteria and no more than 3.");
                return false;
            }

            node.SubNodes = mapper.Map<LinkedList<Node>>(criteria);
            BuildHierarchy(node, node.SubNodes, leafNodes, level + 1, limit, mapper);
            return true;
        });
    }
}

static void DisplayNodes(IEnumerable<Node> nodes, int level, Func<Node, string> message)
{
    string tab = Enumerable.Range(1, level).Aggregate(string.Empty, (space, _) => space + '\t');
    foreach (var node in nodes)
    {
        Console.WriteLine($"{tab}{message(node)}");
        DisplayNodes(node.SubNodes, level + 1, message);
    }
}

static void Repeat(Func<bool> operation)
{
    bool isFinalized = false;
    while (!isFinalized)
    {
        isFinalized = operation();
    }
}
#endregion Methods
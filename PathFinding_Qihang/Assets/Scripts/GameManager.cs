using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public int Size;
    public BoxCollider2D Panel;
    public GameObject token;
    //private int[,] GameMatrix; //0 not chosen, 1 player, 2 enemy de momento no hago nada con esto
    private Node[,] NodeMatrix;
    private int startPosx, startPosy;
    private int endPosx, endPosy;
    void Awake()
    {
        Instance = this;
        //GameMatrix = new int[Size, Size];
        Calculs.CalculateDistances(Panel, Size);
    }
    private void Start()
    {
        /*for(int i = 0; i<Size; i++)
        {
            for (int j = 0; j< Size; j++)
            {
                GameMatrix[i, j] = 0;
            }
        }*/
        
        startPosx = Random.Range(0, Size);
        startPosy = Random.Range(0, Size);
        do
        {
            endPosx = Random.Range(0, Size);
            endPosy = Random.Range(0, Size);
        } while(endPosx== startPosx || endPosy== startPosy);

        //GameMatrix[startPosx, startPosy] = 2;
        //GameMatrix[startPosx, startPosy] = 1;
        NodeMatrix = new Node[Size, Size];
        CreateNodes();

        ExecutePathFinding();
    }
    public void CreateNodes()
    {
        for(int i=0; i<Size; i++)
        {
            for(int j=0; j<Size; j++)
            {
                NodeMatrix[i, j] = new Node(i, j, Calculs.CalculatePoint(i,j));
                NodeMatrix[i,j].Heuristic = Calculs.CalculateHeuristic(NodeMatrix[i,j],endPosx,endPosy);
            }
        }
        for (int i = 0; i < Size; i++)
        {
            for (int j = 0; j < Size; j++)
            {
                SetWays(NodeMatrix[i, j], i, j);
            }
        }
        DebugMatrix();
    }
    public void DebugMatrix()
    {
        for (int i = 0; i < Size; i++)
        {
            for (int j = 0; j < Size; j++)
            {
                Instantiate(token, NodeMatrix[i, j].RealPosition, Quaternion.identity);
                Debug.Log("Element (" + j + ", " + i + ")");
                Debug.Log("Position " + NodeMatrix[i, j].RealPosition);
                Debug.Log("Heuristic " + NodeMatrix[i, j].Heuristic);
                Debug.Log("Ways: ");
                foreach (var way in NodeMatrix[i, j].WayList)
                {
                    Debug.Log(" (" + way.NodeDestiny.PositionX + ", " + way.NodeDestiny.PositionY + ")");
                }
            }
        }
    }
    public void SetWays(Node node, int x, int y)
    {
        node.WayList = new List<Way>();
        if (x>0)
        {
            node.WayList.Add(new Way(NodeMatrix[x - 1, y], Calculs.LinearDistance));
            if (y > 0)
            {
                node.WayList.Add(new Way(NodeMatrix[x - 1, y - 1], Calculs.DiagonalDistance));
            }
        }
        if(x<Size-1)
        {
            node.WayList.Add(new Way(NodeMatrix[x + 1, y], Calculs.LinearDistance));
            if (y > 0)
            {
                node.WayList.Add(new Way(NodeMatrix[x + 1, y - 1], Calculs.DiagonalDistance));
            }
        }
        if(y>0)
        {
            node.WayList.Add(new Way(NodeMatrix[x, y - 1], Calculs.LinearDistance));
        }
        if (y<Size-1)
        {
            node.WayList.Add(new Way(NodeMatrix[x, y + 1], Calculs.LinearDistance));
            if (x>0)
            {
                node.WayList.Add(new Way(NodeMatrix[x - 1, y + 1], Calculs.DiagonalDistance));
            }
            if (x<Size-1)
            {
                node.WayList.Add(new Way(NodeMatrix[x + 1, y + 1], Calculs.DiagonalDistance));
            }
        }
    }

    // MI IMPLEMENTACI�N:

    public void ExecutePathFinding()
    {
        Node startNode = NodeMatrix[startPosx, startPosy];
        Node endNode = NodeMatrix[endPosx, endPosy];
        GameObject entity = null;

        //Realiza el algoritmo A* y devuelve el camino
        List<Node> path = UseAStarAlgorithm(startNode, endNode);

        //Crea nuevos tokens para visualizar el camino
        InstantiateTestTokens(startNode, endNode, ref entity);

        //Ejecuta la animaci�n del camino
        StartCoroutine(ExecutePathAnimation(entity, path));
    }

    public List<Node> UseAStarAlgorithm(Node start, Node end)
    {
        List<Node> result = new List<Node>();
        List<Node> visitedNodes = new List<Node>();
        List<Way> notVisitedWays = new List<Way>();

        Node currentNode = start;
        Way currentWay = new Way(start, 0);
        notVisitedWays.Add(currentWay);

        while (notVisitedWays.Count > 0)
        {
            notVisitedWays = notVisitedWays
                .OrderBy(way => way.ACUMulatedCost + way.NodeDestiny.Heuristic)
                .ToList();

            currentWay = notVisitedWays[0];
            notVisitedWays.RemoveAt(0);
            currentNode = currentWay.NodeDestiny;

            if (currentNode == end)
            {
                while (currentNode != null)
                {
                    result.Add(currentNode);
                    currentNode = currentNode.NodeParent;
                }
                result.Reverse();

                // Para ver cuales son los nodos que ha visitado una vez encontrado el camino
                foreach (var node in visitedNodes)
                {
                    InstantiateToken(node, Color.cyan);
                }
                return result;
            }

            visitedNodes.Add(currentNode);

            foreach (Way newWay in currentNode.WayList)
            {
                Node newNode = newWay.NodeDestiny;
                float newCost = currentWay.ACUMulatedCost + newWay.Cost;

                if (visitedNodes.Contains(newNode))
                    continue;

                // Si el camino no ha sido visitado, lo a�ade a la lista de caminos no visitados
                if (notVisitedWays.FirstOrDefault(way => way.NodeDestiny == newNode) == null)
                {
                    newNode.NodeParent = currentNode;
                    newWay.ACUMulatedCost = newCost;
                    notVisitedWays.Add(newWay);
                }
            }
        }
        return result;
    }

    private void InstantiateTestTokens(Node start, Node end, ref GameObject entityObject)
    {
        InstantiateToken(start, Color.green);
        InstantiateToken(end, Color.green);

        entityObject = Instantiate(token, start.RealPosition, Quaternion.identity);
        entityObject.GetComponent<SpriteRenderer>().color = Color.blue;
    }

    private void InstantiateToken(Node node, Color color)
    {
        GameObject tokenObject = Instantiate(token, node.RealPosition, Quaternion.identity);
        tokenObject.GetComponent<SpriteRenderer>().color = color;
    }

    private IEnumerator ExecutePathAnimation(GameObject entity ,List<Node> nodes)
    {
        foreach (Node node in nodes)
        {
            entity.transform.position = node.RealPosition;

            yield return new WaitForSeconds(0.75f);

            InstantiateToken(node, Color.yellow);
        }
    }

}

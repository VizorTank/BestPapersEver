using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinding 
{
    private const int MoveStrait = 10;
    private const int MoveDiagonal = 14;
    public WorldClass world;

    public static Pathfinding Instance { get; private set; }

    public Pathfinding(WorldClass world)
    {
        this.world = world;
    }
    private List<PathNode> openList;
    private List<PathNode> closedList;


    public List<Vector3> FindPath(Vector3 StartingPosition, Vector3 EndingPosition)
    {
        int startX, startY, startZ, endX, endY, endZ;

        CalculateBlockPoz(StartingPosition, out startX, out startY, out startZ);
        CalculateBlockPoz(EndingPosition, out endX, out endY, out endZ);

        List<PathNode> path = FindPath(startX, startY, startZ, endX, endY, endZ);

        if(path==null)
        { return null; }
        else
        {
            List<Vector3> vectorPath = new List<Vector3>();
            foreach (PathNode pathNode in path)
            {
                vectorPath.Add(new Vector3(pathNode.x, pathNode.y, pathNode.z));
            }
            return vectorPath;
        }
    }


    private List<PathNode> FindPath(int startX, int startY, int startZ, int endX, int endY, int endZ)
    {
        PathNode startNode = new PathNode(startX, startY, startZ);
        PathNode endNode = new PathNode(endX, endY, endZ);

        openList = new List<PathNode> { startNode };
        closedList = new List<PathNode>();



        startNode.gCost = 0;
        startNode.hCost = CalculateDistanceCost(startNode, endNode);
        startNode.CalculateFCost();


        while(openList.Count>0)
        {
            PathNode currentNode = GetLowestFCostNode(openList);
            if(currentNode.x==endNode.x&&currentNode.y==endNode.y&&currentNode.z==endNode.z)
            {
                return CalculatePath(endNode);
            }
            openList.Remove(currentNode);
            closedList.Add(currentNode);

            foreach(PathNode neighbourNode in GetNeighbourList(currentNode))
            {
                if (closedList.Find(x=>x.x==neighbourNode.x&&x.y==neighbourNode.y&&x.z==neighbourNode.z)!=null) continue;  //closedList.Contains(neighbourNode)) continue;

                int tentativeCost = currentNode.gCost + CalculateDistanceCost(currentNode, neighbourNode);
                if(tentativeCost<neighbourNode.gCost)
                {
                    neighbourNode.cameFromNode = currentNode;
                    neighbourNode.gCost = tentativeCost;
                    neighbourNode.hCost = CalculateDistanceCost(neighbourNode, endNode);
                    neighbourNode.CalculateFCost();

                    if(openList.Find(x => x.x == neighbourNode.x && x.y == neighbourNode.y && x.z == neighbourNode.z) == null)
                    {
                        openList.Add(neighbourNode);
                    }
                }
            }

        }
        return null;
    }

    private List<PathNode> GetNeighbourList(PathNode currentNode)
    {
        List<PathNode> neighbourList = new List<PathNode>();
        for(int x=-1;x<=1;x++)
        {
            for(int y=-1;y<=1;y++)
            {
                for(int z=-1;z<=1;z++)
                {
                    if (CalculateNode(currentNode.x + x, currentNode.y + y, currentNode.z + z) != currentNode && CalculateNode(currentNode.x + x, currentNode.y + y, currentNode.z + z)!=null)
                    {
                        PathNode findnode = CalculateNode(currentNode.x + x, currentNode.y + y, currentNode.z + z);
                        findnode.gCost = 99999999;
                        findnode.CalculateFCost();
                        neighbourList.Add(findnode);
                    }
                }
            }
        }
        return neighbourList;
    }


    private PathNode CalculateNode(int x, int y, int z)
    {

       if (!world.GetBlockTypesList().areSolid[world.GetBlock(new Vector3(x, y, z))])
       {
           return new PathNode(x, y, z);
       }
        
        return null;
    }

    private int CalculateDistanceCost(PathNode a, PathNode b)
    {
        int xDistance = Mathf.Abs(a.x - b.x);
        int yDistance = Mathf.Abs(a.y - b.y);
        int zDistance = Mathf.Abs(a.z - b.z);

        int remaining = Mathf.Abs(xDistance - yDistance - zDistance);
        return MoveDiagonal * Mathf.Min(zDistance, yDistance, xDistance) + MoveStrait * remaining;
    }

    public void CalculateBlockPoz(Vector3 poz, out int x, out int y, out int z)
    {
        x = Mathf.FloorToInt(poz.x);
        y = Mathf.FloorToInt(poz.y);
        z = Mathf.FloorToInt(poz.z);

    }


    private PathNode GetLowestFCostNode(List<PathNode> pathNodeList)
    {
        PathNode lowestFCostNode = pathNodeList[0];
        for (int i = 1; i < pathNodeList.Count; i++)
        {
            if (pathNodeList[i].fCost < lowestFCostNode.fCost)
            {
                lowestFCostNode = pathNodeList[i];
            }
        }
        return lowestFCostNode;
    }

    private List<PathNode> CalculatePath(PathNode endNode)
    {
        List<PathNode> path = new List<PathNode>();
        path.Add(endNode);
        PathNode currentNode = endNode;
        while (currentNode.cameFromNode != null)
        {
            path.Add(currentNode.cameFromNode);
            currentNode = currentNode.cameFromNode;
        }
        path.Reverse();
        return path;
    }

}

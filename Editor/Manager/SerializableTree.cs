using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace TinyDataTable
{
    [Serializable]
    public class SerializableTree<ITEM> where ITEM : class
    {
        [Serializable]
        public struct Node : IComparable<Node>
        {
            public string Name;
            public ITEM Item;
            public int Parent;
            public bool IsFolder;

            public int CompareTo(Node other)
            {
                if (other.Name == Name && other.Parent == Parent )
                {
                    return 0;
                }
                return 1;
            }
        }
        [Serializable]
        public class TreeNode
        {
            public Node node;
            public int index;
            public List<TreeNode> children;
        }

        public Node[] Nodes = new Node[0];

        public List<TreeNode> ToTree()
        {
            var nodes =Nodes.Select((n,i) => new TreeNode() { node = n, index = i}).ToArray();
            return ToTree(-1,nodes);
        }
        
        private List<TreeNode> ToTree( int parentIndex , TreeNode[] nodes )
        {
            var roots = nodes
                .Select( (n,i) => (n,i))
                .Where( n => n.n.node.Parent == parentIndex)
                .Select( n => new TreeNode() {node=n.n.node,index=n.i,children=ToTree(n.i, nodes)} )
                .ToList();
            return roots;
        }

        public void FromTree(List<TreeNode> tree)
        {
            List<Node> flatTree = new();
                
            void FlattenTree( List<TreeNode> nodes , int parentID = -1 )
            {
                foreach (var node in nodes)
                {
                    var i = flatTree.Count;
                    flatTree.Add( new Node(){ Name = node.node.Name, Parent = parentID, Item = node.node.Item,IsFolder = node.node.IsFolder} );
                    FlattenTree( node.children , i );
                }
            }
            FlattenTree(tree);
            Nodes = flatTree.ToArray();
        }
    }
}
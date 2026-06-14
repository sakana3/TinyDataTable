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
            public int ID;

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
            public List<TreeNode> children;
        }

        public Node[] Nodes = new Node[0];

        public List<TreeNode> ToTree()
        {
            var nodes =Nodes.Select((n,i) => new TreeNode() { node = n}).ToArray();
            return ToTree(-1,nodes);
        }
        
        private List<TreeNode> ToTree( int parentIndex , TreeNode[] nodes )
        {
            bool check(TreeNode node)
            {
                //条件に合わないアイテムをルートに配置する
                if (parentIndex == -1)
                {
                    //循環参照してしまっている
                    if (node.node.Parent == node.node.ID)
                    {
                        return true;
                    }
                    // 親ノードが存在しない                    
                    if (nodes.Any( n => n.node.ID == node.node.Parent) is false)
                    {
                        return true;
                    }
                }
                return node.node.Parent == parentIndex;
            }
            
            var roots = nodes
                .Select( (n,i) => (n,i))
                .Where( t => check(t.n) )
                .Select( t => new TreeNode() {node=t.n.node,children=ToTree(t.n.node.ID, nodes)} )
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
                    var id = node.node.ID;
                    var newNode = new Node()
                    {
                        Name = node.node.Name,
                        Parent = parentID,
                        Item = node.node.Item,
                        IsFolder = node.node.IsFolder,
                        ID = node.node.ID
                    };
                    flatTree.Add( newNode );
                    FlattenTree( node.children , node.node.ID );
                }
            }
            FlattenTree(tree);
            Nodes = flatTree.ToArray();
        }
    }
}
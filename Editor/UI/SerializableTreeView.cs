using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

    
namespace TinyDataTable.Editor
{
    internal class SerializableTreeView<ITEM> : VisualElement where ITEM : class
    {
        public TreeView treeView { private set; get; }
        public UnityEditor.UIElements.ToolbarSearchField serchField { private set; get; } 
        public SerializableTree<ITEM> target;
        public event Action<List<SerializableTree<ITEM>.TreeNode>> hierarchyChanged;

        public Func<int,SerializableTree<ITEM>.Node,bool,bool, VisualElement> onMakeItem;
        public Action<Rect, Action<string,ITEM>> onCreateItem;
        public Func<ITEM,bool> OnSelectItem;
        public Action<IEnumerable<ITEM>> OnRemoveItem;
        private HelpBox infoBox;
        private bool _isStructureMode;
        public int HotCreateId = -1;

        public SerializableTreeView(SerializableTree<ITEM> target , bool isStructureMode )
        {
            this.target = target;
            this._isStructureMode = isStructureMode;
            CreateGUI();
        }

        private void CreateGUI()
        {
            this.Clear();
            
            serchField = new UnityEditor.UIElements.ToolbarSearchField();
            serchField.style.width = new StyleLength( StyleKeyword.Auto );
            serchField.RegisterValueChangedCallback( OnSearchBarValueChangedCallback );
            this.Add(serchField);


            if (_isStructureMode is false)
            {
                if (target.Nodes == null || target.Nodes.Length == 0)
                {
                    infoBox = new HelpBox("Please switch to structure mode and add a table.", HelpBoxMessageType.Info);
                    Add(infoBox);
                }
            }

            treeView = new TreeView()
            {
                selectionType = SelectionType.Single,
                reorderable = _isStructureMode,
            };
            treeView.style.flexGrow = 1;
            treeView.itemIndexChanged += (_,_) => OnHerarchyChanged();
            treeView.makeItem += () => new VisualElement();
            treeView.bindItem += (element, index) =>
            {
                var id = treeView.GetIdForIndex(index);
                element.Clear();
                if (onMakeItem == null)
                {
                    var label = new Label();
                    label.text = treeView.GetItemDataForId<SerializableTree<ITEM>.TreeNode>(id).node.Name;
                    element.Add(label);
                }
                else
                {
                    var item = treeView.GetItemDataForId<SerializableTree<ITEM>.TreeNode>(id);
                    if (item != null)
                    {
                        var node = treeView.GetItemDataForId<SerializableTree<ITEM>.TreeNode>(id).node;
                        bool isExpand = treeView.viewController.IsExpanded(id);
                        bool hasChildren = treeView.viewController.HasChildren(id);
                        var ve = onMakeItem.Invoke(id, node, isExpand, hasChildren);
                        element.Add(ve);
                    }
                }

                if (_isStructureMode)
                {
                    var itemContextMenu = new ContextualMenuManipulator((e) =>
                        {
                            e.menu.AppendAction("Create Folder", (p) =>
                            {
                                InsertNewTree(id, "New Folder", null, true);
                            });
                            e.menu.AppendAction("Create Table", (p) =>
                            {
                                CreateItem(p.eventInfo.mousePosition, id);
                            });
                            e.menu.AppendAction("Remove", (p) =>
                            {
                                RemoveTree(id);
                            });
                        }
                    ) { target = element };
                }
            };
            if (_isStructureMode)
            {
                var itemContextMenu = new ContextualMenuManipulator((e) =>
                    {
                        e.menu.AppendAction("Create Folder", (p) => { InsertNewTree(-1, "New Folder", null,true); });
                        e.menu.AppendAction("Create Table", (p) => { CreateItem(p.eventInfo.mousePosition, -1); });
                    }
                ) { target = treeView };
            }
            treeView.itemExpandedChanged += args =>
            {
                treeView.RefreshItems();
            };
            treeView.selectedIndicesChanged += indexs =>
            {
                ITEM selected = null;
                if (indexs.Any())
                {
                    var index = indexs.FirstOrDefault();
                    var node = treeView.GetItemDataForIndex<SerializableTree<ITEM>.TreeNode>(index);
                    selected = node.node.Item;
                }
                OnSelectItem?.Invoke(selected);
            };
            treeView.dragAndDropUpdate += args =>
            {
                if (args.parentId != -1)
                {
                    var node = treeView.GetItemDataForId<SerializableTree<ITEM>.TreeNode>(args.parentId);
                    if (node.node.IsFolder is false)
                    {
                        return DragVisualMode.Rejected;
                    }
                }
                
                return DragVisualMode.Move;
            };
//            treeView.setupDragAndDrop += args => Debug.Log("args.selectedIds");            
            
            treeView.fixedItemHeight = 16;
            treeView.viewDataKey = $"SerializableTreeView<{nameof(ITEM)}>";
            Add(treeView);
            
            BuildTree();
        }

        public void BuildTree(SerializableTree<ITEM> item)
        {
            this.target = item;
            if (CheckTree(item))
            {
                RefreshTree(item);
            }
            else
            {
                BuildTree();
            }
        }

        private void RefreshTree(SerializableTree<ITEM> tree)
        {
            for (int i = 0; i < tree.Nodes.Length; i++)
            {
                {
                    var item = treeView.viewController.GetItemForIndex(i) as SerializableTree<ITEM>.TreeNode;
                    item.node = tree.Nodes[i];
                    treeView.RefreshItem(i);
                }
            }
        }

        private void BuildTree()
        {
            var root = MakeTree(target.ToTree());
            treeView.SetRootItems(root);
            treeView.Rebuild();
        }

        public void CreateItem( Vector2 positon, int rootID )
        {
            var mouseRect = new Rect(positon, Vector2.one);
            onCreateItem?.Invoke( mouseRect , (className,item) => InsertNewTree(rootID, className,item,false) );
        }
        
        public void InsertNewTree(int rootID, string name,ITEM nodeItem , bool isFolder )
        {
            if(infoBox != null)
            {
                Remove(infoBox);
                infoBox = null;
            }
            
            int childIndex = -1;

            if (rootID >= 0)
            {
                var t = treeView.viewController.GetItemForId(rootID) as SerializableTree<ITEM>.TreeNode;
                if (t.node.IsFolder)
                {
                    childIndex = -1;
                }
                else
                {
                    childIndex = treeView.viewController.GetChildIndexForId(rootID) + 1;
                    rootID = treeView.viewController.GetParentId(rootID);
                }
            }
            
            var newId = System.Guid.NewGuid().GetHashCode();
            SerializableTree<ITEM>.TreeNode node = new()
            {
                node = new SerializableTree<ITEM>.Node()
                {
                    Name = name,
                    Parent = -1,
                    Item = nodeItem,
                    IsFolder =  isFolder,
                    ID = newId
                },
                children = new(),
            };
            var item = new TreeViewItemData<SerializableTree<ITEM>.TreeNode>(newId,node)
            {
                
            }; 
            treeView.AddItem(item,rootID,childIndex,false);
            HotCreateId = newId;
            treeView.RefreshItems();
            treeView.Rebuild();
            HotCreateId = -1;
            treeView.SetSelectionById( newId );
            int targetIndex = treeView.viewController.GetIndexForId(newId);
            if (targetIndex >= 0)
            {
                treeView.ScrollToItem(targetIndex);
            }
            OnHerarchyChanged();
        }
        

        public void RemoveTree(int id)
        {
            var targetIds = new List<int> { id };
            targetIds.AddRange(GetAllDescendantIds(id));

            var itemsToRemove = targetIds
                .Select(targetId => treeView.GetItemDataForId<SerializableTree<ITEM>.TreeNode>(targetId))
                .Where(node => node?.node.Item != null)
                .Select(node => node.node.Item)
                .ToList();
            
            if (treeView.TryRemoveItem(id, true))
            {
                treeView.RefreshItems();
                treeView.Rebuild();
                OnHerarchyChanged();
                
                OnRemoveItem?.Invoke(itemsToRemove);
            }
        }

        /// <summary>
        /// 特定のツリーID以下のすべてのツリーIDを再帰的に列挙します
        /// </summary>
        public IEnumerable<int> GetAllDescendantIds(int rootId)
        {
            var childrenIds = treeView.viewController.GetChildrenIds(rootId);
            if (childrenIds != null)
            {
                foreach (var childId in childrenIds)
                {
                    yield return childId;
                    foreach (var descendantId in GetAllDescendantIds(childId))
                    {
                        yield return descendantId;
                    }
                }
            }
        }

        public void TreeNameChange( int id , string newName )
        {
            var node = treeView.viewController.GetItemForId(id) as SerializableTree<ITEM>.TreeNode;
            node.node.Name = newName;
            OnHerarchyChanged();
        }

        private void OnHerarchyChanged()
        {
            treeView.RefreshItems();
//            if (treeView.viewController != null)
            {
                var treeNode = TraverseTree();
                hierarchyChanged?.Invoke(treeNode);
            }
            //何故かIDがずれるのでツリーを再構築する
            BuildTree();
        }

        private List<SerializableTree<ITEM>.TreeNode> TraverseTree(int parentIdx = -1)
        {
            return TraverseTree(treeView.viewController.GetRootItemIds(),parentIdx);
        }

        private List<SerializableTree<ITEM>.TreeNode> TraverseTree(IEnumerable<int> root, int parentIdx = -1)
        {
            List<SerializableTree<ITEM>.TreeNode> tree = new();

            foreach (var node in root)
            {
                var treeNode = new SerializableTree<ITEM>.TreeNode();
                var data = treeView.viewController.GetItemForId(node) as SerializableTree<ITEM>.TreeNode;
                var childrenIds = treeView.viewController.GetChildrenIds(node);
                treeNode.node.Name = data.node.Name;
                treeNode.node.Item = data.node.Item;
                treeNode.node.IsFolder = data.node.IsFolder;
                treeNode.node.Parent = parentIdx;
                treeNode.node.ID =  data.node.ID;
                treeNode.children = TraverseTree(childrenIds, data.node.ID);
                tree.Add(treeNode);
            }

            return tree;
        }

        private bool CheckTree(SerializableTree<ITEM> tree)
        {
            var root = treeView.viewController.GetRootItemIds();

            if (root.Count() != tree.Nodes.Length)
            {
                return false;
            }
            
            for (int i = 0; i < tree.Nodes.Length; i++)
            {
                if (tree.Nodes[i].Parent != treeView.viewController.GetParentId(i))
                {
                    return false;
                }
            }

            return true;
        }

        private void OnSearchBarValueChangedCallback( ChangeEvent<string> value)
        {
            var newValue = value.newValue.ToLower();
            for( int i = 0; i < treeView.GetTreeCount(); i++ )
            {
                var item = treeView.GetItemDataForIndex<SerializableTree<ITEM>.TreeNode>(i);
                var name = item.node.Name.ToLower();

                if (String.IsNullOrEmpty(newValue))
                {
                    var element = treeView.GetRootElementForIndex(i);                    
                    element.style.display = DisplayStyle.Flex;
                }
                else if (!name.Contains(newValue) )
                {
                    var element = treeView.GetRootElementForIndex(i);                    
                    element.style.display = DisplayStyle.None;
                }
                else
                {
                    void SetDisp( int index )
                    {
                        var element = treeView.GetRootElementForId(index);                        
                        element.style.display = DisplayStyle.Flex;
                        var parentID = treeView.GetParentIdForIndex(index);
                        if (parentID >= 0)
                        {
                            var parentIndex = treeView.viewController.GetIndexForId(parentID);
                            SetDisp(parentIndex);
                        }
                    }
                    SetDisp(i);
                }
            }
        }

        private List<TreeViewItemData<SerializableTree<ITEM>.TreeNode>> MakeTree(List<SerializableTree<ITEM>.TreeNode> tree)
        {
            var root = tree
                .Select(t => new TreeViewItemData<SerializableTree<ITEM>.TreeNode>(
                    t.node.ID, t, MakeTree(t.children)) { })
                .ToList();
            return root;
        }
    }
}
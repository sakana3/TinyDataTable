using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

public class SplitMultiColumnListView : VisualElement
{
    private static Texture2D plusIcon = EditorGUIUtility.IconContent("Toolbar Plus").image as Texture2D;
    private static Texture2D minusIcon = EditorGUIUtility.IconContent("Toolbar Minus").image as Texture2D;
    
    public MultiColumnListView Left { private set; get; }
    public MultiColumnListView Right { private set; get; }
    public VisualElement LeftContainer { private set; get; }
    public TwoPaneSplitView SplitView { private set; get; }
    private VisualElement footer;

    public Columns LeftColumns => Left.columns;
    public Columns RightColumns => Right.columns;
    
    public UnsignedIntegerField TableSizeField { private set; get; }
    public Action onAddRowClicked;
    public Action onRemoveRowClicked;

    public bool showAddRemoveFooter
    {
        set
        {
            if (value)
            {
                footer = MakeFooter();
                this.Add(footer);
            }
            else
            {
                this.Remove(footer);
                footer = null;
            }
        }
        get => footer != null;
    }
    
    public IList itemsSource
    {
        set
        {
            Left.itemsSource = value;
            Right.itemsSource = value;
        }
        get => Left.itemsSource;
    }

    public bool reorderable
    {
        set => Left.reorderable = value;
        get => Left.reorderable;
    }
    public ColumnSortingMode sortingMode
    {
        set
        {
            Right.sortingMode = value;            
            Left.sortingMode = value;
        }
        get => Left.sortingMode;
    }
    
    public AlternatingRowBackground showAlternatingRowBackgrounds
    {
        set
        {
            Right.showAlternatingRowBackgrounds = value;            
            Left.showAlternatingRowBackgrounds = value;
        }
        get => Left.showAlternatingRowBackgrounds;
    }    

    public ListViewReorderMode reorderMode
    {
        set
        {
            Right.reorderMode = value;            
            Left.reorderMode = value;
        }
        get => Left.reorderMode;
    }               
        
    public bool showBoundCollectionSize
    {
        set
        {
            Right.showBoundCollectionSize = value;            
            Left.showBoundCollectionSize = value;
        }
        get => Left.showBoundCollectionSize;
    }        
    
    public SelectionType selectionType
    {
        set
        {
            Right.selectionType = value;            
            Left.selectionType = value;
        }
        get => Left.selectionType;
    }

    public IEnumerable<int> selectedIndices => Left.selectedIndices;
    
    public event Func<int, int,bool> itemIndexChanged;
    public event Action<float> separatorPositonChanged;
        
    public float SeparatorPositon
    {
        get => SplitView.fixedPane.style.width.value.value;
        set => SplitView.fixedPane.style.width = value;
    }
    
    public SplitMultiColumnListView( float initialDimension = 250f )
    {
        SplitView = new TwoPaneSplitView(0, initialDimension, TwoPaneSplitViewOrientation.Horizontal);
        SplitView.style.backgroundColor = Color.clear;
        
        Add(SplitView);

        Left = new MultiColumnListView();
        var scrollView = Left.Q<ScrollView>();
        if (scrollView != null)
        {
            scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
        }
        LeftContainer = new VisualElement();
        LeftContainer.style.width = initialDimension;

        LeftContainer.Add(Left);
        LeftContainer.RegisterCallback<GeometryChangedEvent>(OnPaneSizeChanged);
        
        SplitView.Add(LeftContainer);
        
        Right = new MultiColumnListView();

        SplitView.Add(Right);

        SyncSelectIndex(Left, Right);
        SyncSelectIndex(Right, Left);

        SyncReordable(Left, Right);
        
        // スクロールの同期設定
        Left.RegisterCallback<AttachToPanelEvent>(evt => SyncScroll(Left, Right,1));
        Right.RegisterCallback<AttachToPanelEvent>(evt => SyncScroll(Right, Left,2));
        
        AddDummuyPadding(Left, Right);
        
        reorderable = true;

    }
    public void Dispose()
    {
        if (SplitView != null)
        {
            // ⚠️ 破棄するときは必ず解除
            SplitView.UnregisterCallback<GeometryChangedEvent>(OnPaneSizeChanged);
        }
    }

    
    public void Rebuild()
    {
        Right.Rebuild();
        Left.Rebuild();
    }

    public void ClearColumns()
    {
        foreach (var column in Right.columns)
        {
            column.bindCell = null;
            column.makeCell = null;
        }
        foreach (var column in Left.columns)
        {
            column.bindCell = null;
            column.makeCell = null;
        }

        Right.columns.Clear();
        Left.columns.Clear();        
    }

    public void ClearSelection()
    {
        Right.ClearSelection();
        Left.ClearSelection();       
    }

    public void RefreshItems()
    {
        Right.RefreshItems();
        Left.RefreshItems();       
    }
    
    
    private void SyncReordable(MultiColumnListView source, MultiColumnListView target)
    {
        source.itemIndexChanged += (i,j) =>
        {
            if (itemIndexChanged != null)
            {
                if (itemIndexChanged.Invoke(i, j))
                {
                    target.itemsSource = source.itemsSource;
                    target.Rebuild();
                }
            }
            else
            {
                target.itemsSource = source.itemsSource;
                target.Rebuild();
            }

            source.schedule.Execute(() =>
            {
                target.SetSelection(source.selectedIndices);
            });
        };
    }
    
    private void SyncSelectIndex(MultiColumnListView source, MultiColumnListView target)
    {
        bool seleted = false;
        //行の選択を同期させる
        source.selectedIndicesChanged += (index) =>
        {
            if (seleted == false)
            {
                seleted = true;
                target.SetSelection(index);
                seleted = false;
            }
        };        
    }

    private void AddDummuyPadding(MultiColumnListView source, MultiColumnListView target)
    {
        var sourceScrollView = source.Q<ScrollView>();
        var targetScrollView = target.Q<ScrollView>();

        var srcScrollContaint = sourceScrollView.Q<VisualElement>("unity-content-and-vertical-scroll-container");
        
        targetScrollView.Q<ScrollView>().horizontalScroller.RegisterCallback<GeometryChangedEvent>(evt =>
        {
            if (evt.newRect.height != 0)
            {
                srcScrollContaint.style.marginBottom = evt.newRect.height;
            }
            else
            {
                srcScrollContaint.style.marginBottom = 0;
            }
        });
    }
    
    private bool isScrollChanged = false;
    private void SyncScroll(MultiColumnListView source, MultiColumnListView target , int id)
    {
        var sourceScrollView = source.Q<ScrollView>();
        var targetScrollView = target.Q<ScrollView>();

        // MultiColumnListView が内部に ScrollView を持つまで待つ必要があるため
        // スケジューラーを使用して ScrollView の取得を試みる
        source.schedule.Execute(() =>
        {
            if (sourceScrollView != null && targetScrollView != null)
            {
                sourceScrollView.verticalScroller.valueChanged += (value) =>
                {
                    if (isScrollChanged)
                    {
                        isScrollChanged = false;
                        return;
                    }
                    isScrollChanged = true;
                    targetScrollView.verticalScroller.value = value;
                };
            }
        }).Until(() => source.Q<ScrollView>() != null && target.Q<ScrollView>() != null);
        
        sourceScrollView.horizontalScroller.RegisterCallback<GeometryChangedEvent>(evt =>
        {
        });
    }

    private VisualElement MakeFooter()
    {
        MethodInfo addField = typeof(BaseListView)
            .GetMethod("OnAddClicked", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo removeield = typeof(BaseListView)
            .GetMethod("OnRemoveClicked", BindingFlags.NonPublic | BindingFlags.Instance);
        
        var footer = new VisualElement();
        footer.name = BaseListView.footerUssClassName;
        footer.AddToClassList(BaseListView.footerUssClassName);        
        
        var TableSizeField = new UnsignedIntegerField()
        {
        };
        TableSizeField.SendToBack();
        TableSizeField.style.marginRight = 4.0f;
        
        var addButton = new Button()
        {
            name = BaseListView.footerAddButtonName,
        };
        addButton.iconImage = plusIcon;
        addButton.clicked += () =>
        {
            onAddRowClicked?.Invoke();
            Rebuild();
        };
        
        var removeButton = new Button()
        {
            name = BaseListView.footerRemoveButtonName,
            iconImage = minusIcon
        };
        removeButton.clicked += () =>
        {
            onRemoveRowClicked?.Invoke();
            Rebuild();
        };

        ApplyFooterButtonStyle(addButton);
        ApplyFooterButtonStyle(removeButton);

        footer.Add(TableSizeField);        
        footer.Add(addButton);
        footer.Add(removeButton);

        return footer;        
    }
    private void OnPaneSizeChanged(GeometryChangedEvent evt)
    {
        if (evt.newRect.width != evt.oldRect.width)
        {
            separatorPositonChanged?.Invoke(evt.newRect.width);
        }
    }

    // ボタンの見た目を整える補助メソッド
    private void ApplyFooterButtonStyle(Button btn)
    {
        btn.style.width = 25;
        btn.style.height = 20;
        btn.style.marginRight = 2;
        btn.style.marginTop = 1;
        btn.style.marginBottom = 1;
    }    
}

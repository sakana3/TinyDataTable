using System;
using System.Reflection;
using System.Collections;
using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

public class SpliteMultiColumnListView : VisualElement
{
    private static Texture2D plusIcon = EditorGUIUtility.IconContent("Toolbar Plus").image as Texture2D;
    private static Texture2D minusIcon = EditorGUIUtility.IconContent("Toolbar Minus").image as Texture2D;
    
    public MultiColumnListView Left { private set; get; }
    public MultiColumnListView Right { private set; get; }
    public VisualElement LeftContainer { private set; get; }
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
    
    public SpliteMultiColumnListView()
    {
        var splite = new TwoPaneSplitView();
        splite.style.backgroundColor = Color.clear;
        
        Add(splite);

        Left = new MultiColumnListView();
        var scrollView = Left.Q<ScrollView>();
        if (scrollView != null)
        {
            scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
        }
        LeftContainer = new VisualElement();
//        LeftContainer.style.marginBottom = 13.3f;
        LeftContainer.Add(Left);
        
        splite.Add(LeftContainer);
        
        Right = new MultiColumnListView();

        splite.Add(Right);

        SyncSelectIndex(Left, Right);
        SyncSelectIndex(Right, Left);

        SyncReordable(Left, Right);
//        SyncReordable(Right, Left);
        
        // スクロールの同期設定
        Left.RegisterCallback<AttachToPanelEvent>(evt => SyncScroll(Left, Right,1));
        Right.RegisterCallback<AttachToPanelEvent>(evt => SyncScroll(Right, Left,2));
        
        AddDummuyPadding(Left, Right);
        
        reorderable = true;

    }

    
    public void Rebuild()
    {
        Right.Rebuild();
        Left.Rebuild();
    }

    private void SyncReordable(MultiColumnListView source, MultiColumnListView target)
    {
        source.itemIndexChanged += (i,j) =>
        {
            target.itemsSource = source.itemsSource;
            target.Rebuild();
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
        var targetScrollContaint = targetScrollView.Q<VisualElement>("unity-content-and-vertical-scroll-container");
        
        source.schedule.Execute(() =>
        {
            if (sourceScrollView != null && targetScrollView != null)
            {
            }
        }).Until(() => source.Q<ScrollView>() != null && target.Q<ScrollView>() != null);   
        targetScrollView.Q<VisualElement>("unity-content-and-vertical-scroll-container").RegisterCallback<GeometryChangedEvent>(evt =>
        {
            var diff = srcScrollContaint.contentRect.height - targetScrollContaint.contentRect.height;
            if (diff > 0)
            {
                srcScrollContaint.style.marginBottom = diff;
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
        // スタイル調整（Unity標準のボタンっぽく見せる）
        ApplyFooterButtonStyle(addButton);
        ApplyFooterButtonStyle(removeButton);

        footer.Add(TableSizeField);        
        footer.Add(addButton);
        footer.Add(removeButton);

        return footer;        
    }

    public void RebuildBoth()
    {
        Right.Rebuild();
        Left.Rebuild();
    }

    // ボタンの見た目を整える補助メソッド
    void ApplyFooterButtonStyle(Button btn)
    {
        btn.style.width = 25;
        btn.style.height = 20;
        btn.style.marginRight = 2;
        btn.style.marginTop = 1;
        btn.style.marginBottom = 1;
        // 背景を透明にして枠線を消すと、より標準のフッターに近くなります
    }    
}

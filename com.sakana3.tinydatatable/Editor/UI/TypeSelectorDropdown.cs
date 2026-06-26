using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
#if USE_ADDRESSABLES
using UnityEngine.AddressableAssets;
#endif

namespace TinyDataTable.Editor
{
    internal class TypeSelectorDropdown : AdvancedDropdown
    {
        private readonly Action<Type> _onTypeSelected;
        private readonly IEnumerable<string> _assemblys;

        //Unityの代表的な型
        private static Type[] types = new[]
        {
            typeof(int),
            typeof(float),
            typeof(bool),
            typeof(string),
            typeof(long),
            typeof(double),
            typeof(char),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(uint),
            typeof(ulong),
        };

        private static Type[] builtinTypes = new[]
        {
            typeof(Vector2),
            typeof(Vector2Int),
            typeof(Vector3),
            typeof(Vector3Int),
            typeof(Vector4),
            typeof(Quaternion),
            typeof(Matrix4x4),
            typeof(Color),
            typeof(Color32),
            typeof(Gradient),
            typeof(Rect),
            typeof(RectInt),
            typeof(Bounds),
            typeof(BoundsInt),
            typeof(Plane),
            typeof(LayerMask),
            typeof(AnimationCurve),
#if USE_ADDRESSABLES
            typeof(AssetReference),
#endif
        };

        private static Type[] _enumTypes = null;
        private static Type[] _dataTableTypes = null;
        private static Type[] _classTypes = null;
        private static Type[] _scriptableObjectTypes = null;
        private static Type[] _monoBeheiborTypes = null;

        public TypeSelectorDropdown(AdvancedDropdownState state, IEnumerable<string> assemblys, Action<Type> onTypeSelected) :
            base(state)
        {
           
            _assemblys = assemblys;
            _onTypeSelected = onTypeSelected;

            if (_enumTypes == null || _classTypes == null|| _dataTableTypes == null|| _scriptableObjectTypes == null )
            {
                (_enumTypes, _classTypes,_dataTableTypes,_scriptableObjectTypes,_monoBeheiborTypes) = CollectTypes();
            }
            
            // ウィンドウサイズの最小値を設定
            minimumSize = new Vector2(200, 300);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Types");

            foreach (var type in types)
            {
                AddTypeItem(root, type, false);
            }

            var typeRoot = new AdvancedDropdownItem("Unity Types");
            foreach (var type in builtinTypes)
            {
                AddTypeItem(typeRoot, type, false);
            }
            root.AddChild(typeRoot);

            var soRoot = new AdvancedDropdownItem("Unity Object");
            foreach (var type in _scriptableObjectTypes)
            {
                AddTypeItem(soRoot, type, false);
            }
            root.AddChild(soRoot);
            
            var monoRoot = new AdvancedDropdownItem("Component");
            foreach (var type in _monoBeheiborTypes)
            {
                AddTypeItem(monoRoot, type, false);
            }
            root.AddChild(monoRoot);

            
            if (_dataTableTypes.Any())
            {
                var dataTableRoot = new AdvancedDropdownItem("DataTable");
                foreach (var type in _dataTableTypes)
                {
                    AddTypeItem(dataTableRoot, type, false);
                }

                root.AddChild(dataTableRoot);
            }

            var enumRoot = new AdvancedDropdownItem("Enum");
            foreach (var type in _enumTypes)
            {
                AddTypeItem(enumRoot, type, true);
            }
            root.AddChild(enumRoot);
            
            var classRoot = new AdvancedDropdownItem("Class");
            foreach (var type in _classTypes)
            {
                AddTypeItem(classRoot, type, true);
            }
            root.AddChild(classRoot);

            return root;
        }

        private void AddTypeItem(AdvancedDropdownItem root, Type type, bool isNest)
        {
            // シンプルに型名だけで追加する場合
            // var item = new TypeDropdownItem(type.Name, type);
            // root.AddChild(item);

            // 名前空間で階層化する場合
            var parent = root;
            if (isNest)
            {
                var parts = type.FullName.Split('.');
                foreach (var part in parts.SkipLast(1))
                {
                    var child = parent.children.FirstOrDefault(c => c.name == part);
                    if (child == null)
                    {
                        child = new AdvancedDropdownItem(part)
                        {
                            icon = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D
                        };
                        parent.AddChild(child);
                    }

                    parent = child;
                }
                
                var subparts = parts.Last().Split('+');
                if (subparts.Length > 1)
                {
                    foreach (var subpart in subparts.SkipLast(1))
                    {
                        var part = subpart+".";
                        var child = parent.children.FirstOrDefault(c => c.name == part);
                        if (child == null)
                        {
                            child = new AdvancedDropdownItem(part)
                            {
                                icon = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D
                            };
                            parent.AddChild(child);
                        }

                        parent = child;

                    }
                }
            }

            var item = new TypeDropdownItem(type.GetCSharpAlias(), type);
            parent.AddChild(item);
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is TypeDropdownItem typeItem)
            {
                _onTypeSelected?.Invoke(typeItem.Type);
            }
        }

        // 型情報を保持するためのカスタムアイテムクラス
        private class TypeDropdownItem : AdvancedDropdownItem
        {
            public Type Type { get; }

            public TypeDropdownItem(string name, Type type) : base(name)
            {
                Type = type;
                icon = EditorGUIUtility.ObjectContent(null, type).image as Texture2D; // 型のアイコンがあれば設定
            }
        }

        private (Type[] enumTypes,Type[] classTypes,Type[] dataTableTypes,Type[] scriptableObjectTypes,Type[] monoBeheiborTypes)  CollectTypes()
        {
            var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(t => _assemblys.Contains(t.GetName().Name))
                .SelectMany(a => a.GetTypes())
                .Where( t => 
                    t.IsPublic &&
                    t.IsDefined(typeof(ObsoleteAttribute), true) is false )
                .SelectMany( t => GetAllNestedTypesRecursive(t) )
                .Where(t => SerializableUtility.CheckUnitySerializable(t))
                .ToArray();
            
            var enumTypes = allTypes
                .Where(t => t.IsEnum && t.IsSerializable)
                .ToArray();

            var allClassTypes= allTypes
                .Where(t => !t.IsInterface && !t.IsEnum && !t.IsPrimitive && !t.IsArray && t != typeof(void))
                .Where(t => t.IsClass || t.IsValueType )
                .Where( t => typeof(UnityEngine.Object).IsAssignableFrom(t) is false )
                .ToArray();
            
            var classTypes = allClassTypes
                .Where( t => !typeof(IIdentifier).IsAssignableFrom(t))
                .ToArray();
            
            var dataTableTypes = allClassTypes
                .Where( t => typeof(IIdentifier).IsAssignableFrom(t))
                .ToArray();

            var scriptableObjectTypes = allTypes
                .Where( t => typeof(UnityEngine.Object).IsAssignableFrom(t))
                .Where( t => typeof(UnityEngine.Component).IsAssignableFrom(t) is false)
                .ToArray();

            var monoBeheiborTypes = allTypes
                .Where( t => typeof(UnityEngine.Object).IsAssignableFrom(t))
                .Where( t => typeof(UnityEngine.Component).IsAssignableFrom(t) is true)
                .ToArray();
            
            return (enumTypes,classTypes,dataTableTypes,scriptableObjectTypes,monoBeheiborTypes);
        }
        
        // 再帰的にネストされた型を掘り下げるメソッド
        static List<Type> GetAllNestedTypesRecursive(Type currentType)
        {
            List<Type> resultList = new List<Type>();
            resultList.Add(currentType);
            GetAllNestedTypesRecursive(currentType, resultList);
            return resultList;
        }

        static void GetAllNestedTypesRecursive(Type currentType, List<Type> resultList)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic;
            Type[] nestedTypes = currentType.GetNestedTypes(flags);

            foreach (Type nested in nestedTypes)
            {
                resultList.Add(nested);
            
                // このネスト型の中に、さらにネスト型がないか深く潜る
                GetAllNestedTypesRecursive(nested, resultList);
            }
        }        
    }

    internal class AssemblieSelectorDropdown : AdvancedDropdown
    {
        private static Assembly[] allAssembly = System.AppDomain.CurrentDomain.GetAssemblies()
            .ToArray();
        // 型情報を保持するためのカスタムアイテムクラス
        private class Item : AdvancedDropdownItem
        {
            public Assembly assembly { get; private set; }
            public Item(Assembly asm) : base(asm.GetName().Name)
            {
                assembly = asm;
            }
        }
        
        private Action<Assembly> onTypeSelected;

        public AssemblieSelectorDropdown( 
            AdvancedDropdownState state,
            string name,
            Action<Assembly> onTypeSelected ) : base(state)
        {
            minimumSize = new Vector2(200, 300);
            this.onTypeSelected = onTypeSelected;
        }
        
        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is Item typeItem)
            {
                onTypeSelected?.Invoke(typeItem.assembly);
            }
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Types");
            foreach (var asm in allAssembly)
            {
                var item = new Item(asm);
                root.AddChild(item);
            }
            return root;
        }
    }    
}

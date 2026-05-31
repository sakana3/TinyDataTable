using System;
using System.Reflection;
using UnityEngine;

namespace TinyDataTable
{
    [CreateAssetMenu(fileName = "NewDataTable", menuName = "TinyDataTable/NewDataTable")]
    public class DataTableAsset : ScriptableObject
    {
        /// <summary> DataTable </summary>
        [SerializeReference] private IRecord record;

        /// <summary> Class Type </summary>
        public Type ClassType => record?.ClassType;

        /// <summary> Record Type </summary>
        public Type RecordType => record?.RecordType;

        /// <summary> Record Data </summary>
        public IRecord Record => record;
        
#if UNITY_EDITOR
        /// <summary> Class Script </summary>
        [SerializeField]
        public UnityEditor.MonoScript classScript;
        
        /// <summary> Obsolete </summary>
        [SerializeField]
        private bool obsolete;
        public bool Obsolete
        {
            get => obsolete;
            set => obsolete = value;
        }

        /// <summary> InitializeOnLoad </summary>
        [SerializeField] public bool InitializeOnLoad = true;

        /// <summary> InitializeOnLoadEditor </summary>
        [SerializeField]  public bool InitializeOnLoadEditor = true;

        public void Bind(Type classType, Type recordType)
        {
            var genericDefinition = typeof(DataTableRecord<,>);
            var constructedType = genericDefinition.MakeGenericType(classType,recordType);
            if (record == null)
            {
                record = Activator.CreateInstance(constructedType) as IRecord;
                record?.Initialize();
            }
        }

        public static string nameOfRecord => nameof(DataTableAsset.record);

#endif
        private void Reset()
        {
        }

        private void OnEnable()
        {
            if (ClassType != null)
            {
                MethodInfo method = ClassType.GetMethod("BindAsset", BindingFlags.NonPublic | BindingFlags.Static);
                method?.Invoke(null, new object[] { this });
            }
        }

        private void OnDisable()
        {
            if (ClassType != null)
            {
                MethodInfo method = ClassType.GetMethod("BindAsset", BindingFlags.NonPublic | BindingFlags.Static);
                method?.Invoke(null, new object[] { null });
            }
        }
    }
}
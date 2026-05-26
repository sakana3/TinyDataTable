using System;
using System.Linq;

namespace TinyDataTable
{
    public static partial class VariableStructBuilder
    {
        public static IVariableStruct Instantiate(params Type[] typeArgument)
        {
            Type constructedType;            
            if (typeArgument.Any() )
            {
                //型引数に応じたGeneric型を取得
                var genericDefinition = VariableStructBuilder.GetType(typeArgument.Length);
                //MakeGenericTypeで型を生成
                constructedType = genericDefinition.MakeGenericType(typeArgument);
            }
            else
            {
                constructedType = typeof(VariableStruct);
            }            
            // インスタンス化
            IVariableStruct instance = Activator.CreateInstance(constructedType) as IVariableStruct;
            return instance;            
        }
    }
}
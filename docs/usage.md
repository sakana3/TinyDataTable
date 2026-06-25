## 1. データの定義 (C#)

まず、読み込みたいデータの構造に合わせて、C#のクラス（または構造体）を定義します。

```csharp
using System;

[Serializable]
public class EnemyData
{
    public int id;
    public string name;
    public int hp;
    public int atk;
}
using SerializerScript;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SimplexSerializerProtobuffer : MonoBehaviour
{
    public void OnEnable()
    {
        Test();
    }

    void Test()
    {
        ABC bC = new ABC();
        bC.abcsss.a = 10;
        bC.abcsss.b = 20;
        bC.abcsss.a1 = 1;
        bC.abcsss.a2 = 2;
        bC.abcsss.a3 = 3;
        bC.abcsss.a4 = 4;
        bC.abcsss.a5 = 5;
        bC.abcsss.a6 = 6;
        bC.abcsss.a7 = 7;
        bC.abcsss.a8 = true;
        bC.abcsss.a9 = false;

        for (int i = 0; i < 2; i++)
            bC.BCSSSes.Add(new ABCSSS() { a = 30, b = 42, c = i });
        bC.BCSSSes.Add(null);
        bC.BCSSSes.Add(new ABCSSS() { a = 35, b = 40 });
        bC.BCSSSes.Add(null);
        bC.baseAbc.Add(25);
        bC.baseAbc.Add(27);
        bC.baseAbc.Add(28);
        bC.baseAbc.Add(29);
        bC.teststr = "abcd";

        Debug.LogError("Serilaizer before:" + bC.ToString());
        var bytes = SerializerHelper.Serilaizer(bC);
        ABC b2 = SerializerHelper.Deserilaizer<ABC>(bytes);
        Debug.LogError("Serilaizer after   :" + bC.ToString());

    }

}

[AutoGeneraterSerializerCode]
public class ABCSSS
{
    [SerializerNumber(1)]
    public int a;
    [SerializerNumber(2)]
    public int b;

    [SerializerNumber(3)]
    public float c;

    [SerializerNumber(4)]
    public double a1;

    [SerializerNumber(5)]
    public ulong a2;

    [SerializerNumber(6)]
    public long a3;

    [SerializerNumber(7)]
    public sbyte a4;

    [SerializerNumber(8)]
    public byte a5;

    [SerializerNumber(9)]
    public short a6;

    [SerializerNumber(10)]
    public ushort a7;

    [SerializerNumber(12)]
    public bool a8;

    [SerializerNumber(13)]
    public bool a9;

    [SerializerNumber(15)]
    public EenumA eenumA;

    public override string ToString()
    {
        string str = "{";
        str += "a:" + a + ",";
        str += "b:" + b + ",";
        str += "c:" + c + ",";
        str += "a1:" + a1 + ",";
        str += "a2:" + a2 + ",";
        str += "a3:" + a3 + ",";
        str += "a4:" + a4 + ",";
        str += "a5:" + a5 + ",";
        str += "a6:" + a6 + ",";
        str += "a7:" + a7 + ",";
        str += "a8:" + a8 + ",";
        str += "a9:" + a9 + ",";
        str += "eenumA:" + eenumA + ",";
        str += "}";
        return str;
    }
}

[AutoGeneraterSerializerCode]
public class ABC
{
    [SerializerNumber(3)]
    public List<ABCSSS> BCSSSes = new List<ABCSSS>();

    [SerializerNumber(2)]
    public List<int> baseAbc = new List<int>();

    [SerializerNumber(1)]
    public ABCSSS abcsss = new ABCSSS();

    [SerializerNumber(5)]
    public string teststr = "12345";

    [SerializerNumber(6)]
    public ABC aBC = null;

    public override string ToString()
    {
        string str = "{";
        str += "teststr " + teststr + ",";
        str += "abcsss " + abcsss.ToString() + ",";
        for (int i = 0; i < BCSSSes.Count; i++)
            if (BCSSSes[i] != null)
                str += "BCSSSes " + i + ":" + BCSSSes[i].ToString() + ",";
        str += "}";
        return str;
    }
}

public class VInt2
{
    public int x;
    public int y;
}


public class VInt3
{
    public int x;
    public int y;
    public int z;
}

public enum EenumA
{
    a, b, c
}



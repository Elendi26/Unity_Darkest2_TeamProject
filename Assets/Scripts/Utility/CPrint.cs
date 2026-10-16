using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CPrint : MonoBehaviour
{
    public static bool Enable = true;

    public static bool EnableRichText = true;

    private static int _indentLevel = 0;
    private const int INDENT_SPACES = 2;

    private static readonly HashSet<string> _onceSet = new HashSet<string>();

    private static string Indent
    {
        get
        {

            return new string(' ', _indentLevel * INDENT_SPACES);
        }
    }

    public static void IndentPush()
    {

        _indentLevel++;
    }

    public static void IndentPop()
    {
        _indentLevel--;

        if (_indentLevel < 0)
        {
            _indentLevel = 0;
        }
    }

    public static void IndentReset()
    {
        _indentLevel = 0;
    }

    private enum ELogKind
    {
        Log,
        Warn,
        Error,
        Success
    }

    private static void Emit(ELogKind kind, string msg, string tag = null, string colorHex = null)
    {


        if (!Enable)
        {
            return;
        }

        string prefix = string.Empty;

        if (!string.IsNullOrEmpty(tag))
        {
            if (EnableRichText && !string.IsNullOrEmpty(colorHex))
            {

                prefix = $"<color={colorHex}> [{tag}]</color>";
            }
            else
            {

                prefix = $"[{tag}]";
            }
        }

        string final = $"{Indent}{prefix}{msg}";

        switch (kind)
        {
            case ELogKind.Log:


            case ELogKind.Success:
                Debug.Log(final);
                break;

            case ELogKind.Warn:
                Debug.LogWarning(final);
                break;

            case ELogKind.Error:
                Debug.LogError(final);
                break;

        }


    }

    public static void Title(string title, char LineCH = '=')
    {
        Line(LineCH);
        Emit(ELogKind.Log, title);
        Line(LineCH);
    }

    public static void Section(string section, char LineCH = '=')
    {
        Emit(ELogKind.Log, section);
        Line(LineCH);
    }

    public static void Line(char ch = '-', int count = 10)
    {
        Emit(ELogKind.Log, new string(ch, count));
    }

    public static void Blank(int lines = 1)
    {
        if (!Enable)
        {
            return;
        }

        // 빈줄 여러 줄
        if (lines <= 0)
        {
            return;
        }

        Debug.Log(new string('\n', lines));

    }

    public static void Log(string msg)
    {
        Emit(ELogKind.Log, msg);
    }
    public static void Warn(string msg)
    {
        // 주황색
        Emit(ELogKind.Warn, msg, "WARN", "#FF9100");
    }
    public static void Error(string msg)
    {
        // 빨간색
        Emit(ELogKind.Error, msg, "ERROR", "#FF1744");
    }

    public static void Success(string msg)
    {
        // 초록색
        Emit(ELogKind.Success, msg, "OK", "#00C853");
    }

    public static void Assert(bool condition, string msg)
    {
        if (condition)
        {
            return;
        }

        Error($"[ASSERT] {msg}");

    }

    public static void CheckNull(object obj, string msg)
    {
        if (obj != null)
        {
            return;
        }
        Warn($"[NULL] {msg}");
    }

    public static T Ref<T>(T obj, string msg) where T : class
    {



        if (obj == null)
        {
            Warn($"[NULL] {msg}");
        }


        return obj;
    }


    public static void V3(string label, Vector3 v, int digits = 2)
    {

        float x = (float)System.Math.Round(v.x, digits);
        float y = (float)System.Math.Round(v.y, digits);
        float z = (float)System.Math.Round(v.z, digits);

        Emit(ELogKind.Log, $"{label} : ({x}, {y}, {z})");


    }

    public static void KV(string key, object value)
    {

        Log($"{key} = {value}");

    }

    public static void Group(string title, Action body, char lineCh = '=', int LineCount = 20)
    {
        if (!Enable)
        {
            return;
        }

        // 1. 그룹 제목 출력
        Title(title, lineCh);
        // 2. 그룹 내부 들여쓰기
        IndentPush();
        // 실행할 코드 블록을 호출 (Action)
        // ?.Invoke() : body가 null이면 실행하지 않겠다 -> 예외 방지
        body?.Invoke();
        // 다시 복구 (들여쓰기)
        IndentPop();
        // 구분선 -> 스타일 튜닝
        Line(lineCh, LineCount);

    }

    public static void Once(string key, string msg)
    {
        if (!Enable)
        {
            return;
        }

        if (_onceSet.Contains(key)) // 이미 키가 있는 경우 -> 재출력 금지
        {
            return;
        }

        _onceSet.Add(key);

        Warn($"[ONCE] {msg}");


    }

    public static void OnceClear()
    {
        // 등록된 키 전부 비운다.

        _onceSet.Clear();
    }


    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEBELOPMENT_BUILD")]
    public static void Ray(Vector3 origin, Vector3 direction, Color color, float duration = 0f)
    {
        if (Enable)
        {
            return;
        }

        Debug.DrawRay(origin, direction, color, duration);

    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEBELOPMENT_BUILD")]
    public static void Line3D(Vector3 a, Vector3 b, Color color, float duration = 0f)
    {
        if (Enable)
        {
            return;
        }

        Debug.DrawLine(a, b, color, duration);

    }
}

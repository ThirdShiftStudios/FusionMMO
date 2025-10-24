using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ParabolaString : MonoBehaviour
{
    //Parabola code source : https://forum.unity.com/threads/generating-dynamic-parabola.211681/#post-1426169
    public LineRenderer rodString;
    public int arabolaStep;
    public Transform someObject; //object that moves along parabola.
    float objectT = 0; //timer for that object

    public Transform Ta, Tb; //transforms that mark the start and end
    public float h; //desired parabola height

    Vector3 a, b; //Vector positions for start and end
    private void Start()
    {
        rodString.positionCount = arabolaStep;
    }
    void Update()
    {
        if (Ta && Tb ) {
            a = Ta.position; //Get vectors from the transforms
            b = Tb.position;

            if (someObject)
            {
                //Shows how to animate something following a parabola
                objectT = Time.time % 1; //completes the parabola trip in one second
                someObject.position = SampleParabola(a, b, h, objectT);
            }
        }

        //for (int i = 0; i < 5; i++)
        //{
        //    rodString.SetPosition(i, SampleParabola(a, b, h, objectT));
        //}

        float count = arabolaStep - 1;
        Vector3 lastP = a;
        for (float i = 0; i < count + 1; i++)
        {
            Vector3 p = SampleParabola(a, b, h, i / count);
            rodString.SetPosition((int)i, p);
            lastP = p;
        }
    }


    void OnDrawGizmos()
    {
#if UNITY_EDITOR
        //Draw the height in the viewport, so i can make a better gif :]
        Handles.BeginGUI();
        GUI.skin.box.fontSize = 16;
        GUI.Box(new Rect(10, 10, 100, 25), h + "");
        Handles.EndGUI();

        //Draw the parabola by sample a few times
        Gizmos.color = Color.red;
        Gizmos.DrawLine(a, b);
        float count = arabolaStep;
        Vector3 lastP = a;
        for (float i = 0; i < count + 1; i++)
        {
            Vector3 p = SampleParabola(a, b, h, i / count);
            Gizmos.color = i % 2 == 0 ? Color.blue : Color.green;
            Gizmos.DrawLine(lastP, p);
            lastP = p;
        }
#endif
    }

    #region Parabola sampling function
    /// <summary>
    /// Get position from a parabola defined by start and end, height, and time
    /// </summary>
    /// <param name='start'>
    /// The start point of the parabola
    /// </param>
    /// <param name='end'>
    /// The end point of the parabola
    /// </param>
    /// <param name='height'>
    /// The height of the parabola at its maximum
    /// </param>
    /// <param name='t'>
    /// Normalized time (0->1)
    /// </param>S
    Vector3 SampleParabola(Vector3 start, Vector3 end, float height, float t)
    {
        if (Mathf.Abs(start.y - end.y) < 0.1f)
        {
            //start and end are roughly level, pretend they are - simpler solution with less steps
            Vector3 travelDirection = end - start;
            Vector3 result = start + t * travelDirection;
            result.y += Mathf.Sin(t * Mathf.PI) * height;
            return result;
        }
        else
        {
            //start and end are not level, gets more complicated
            Vector3 travelDirection = end - start;
            Vector3 levelDirection = end - new Vector3(start.x, end.y, start.z);
            Vector3 right = Vector3.Cross(travelDirection, levelDirection);
            //Vector3 up = Vector3.Cross(right, travelDirection); //대칭형
            Vector3 up = Vector3.Cross(right, levelDirection); //중력형
            if (end.y > start.y) up = -up;
            Vector3 result = start + t * travelDirection;
            result += (Mathf.Sin(t * Mathf.PI) * height) * up.normalized;
            return result;
        }
    }
    #endregion
}

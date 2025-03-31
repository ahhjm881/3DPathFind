using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplineFollower : MonoBehaviour
{
    [Header("Progress")]                         
    public float t;                              // 현재 진행 상황
                                                 
    [Header("Movement")]                         
    public float angulerSpeed;                   // 회전 속도
    public float speed;                          // 움직임 속도
    public bool move;                            // 이동 여부, false 시 t 값 변동에도 위치가 변하지 않음
                                                 
    [Header("Setting")]                          
    [Range(0, 1)] public float tension;          // 텐션 값, Cardinal type 에만 동작
    [Range(1, 50)] public int iteration=10;      // 정밀도
    public bool useLooks;                        // note의 look을 바라보게 할건지에 대한 전역 설정
    public bool repeat;                          // 최종 목적지에 도달 시 처음으로 돌아가게 할건지 (t = 0)
    public bool capture;
    public SplineType type;                      // 스플라인 타입 설정
                                                 
    [Header("Debug")]                            
    public bool drawHandles;                     // 기즈모 핸들을 보이게 할건지

    [Space]                                      
    public List<SplineNote> notes;               // 스플라인 노드

    public bool isEnd { get; private set; }


    private void Awake()
    {
        targets = PathAdapter.NoteCapture(notes, tension, iteration);
    }

    private void Update()
    {
        if (!move) return;

        if (capture)
            Capture();
        else
            Spline();
    }

    int index;
    List<Vector3> targets;
    private void Capture()
    {
        if (index >= targets.Count)
        {
            if (repeat)
                index = 0;
            else
                return;
        }

        Vector3 v = Vector3.MoveTowards(transform.position, targets[index], Time.deltaTime * speed);

        if (Vector3.Distance(targets[index], transform.position) <= 0.1f)
        {
            index++;
        }

        Vector3 dir = v - transform.position;
        transform.position = v;


        if (dir != Vector3.zero)
        {
            Quaternion q;

            q = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, q, Time.deltaTime * angulerSpeed);
        }
    }

    private void Spline()
    {
        if (notes.Count <= (int)t + 1)
        {
            if (repeat) t = 0f;
            else
            {
                isEnd = true;
                return;
            }
        }
        else
        {
            isEnd = false;
        }

        if (!move) return;
        if (t < 0) t = 0f;
        if (speed <= 0f) speed = 0f;


        var v = CustomSpline.MoveToWard(notes[(int)t], notes[(int)t + 1], (t - notes[(int)t].index), type, tension);

        Vector3 dir = v - transform.position;
        transform.position = v;

        t += speed * Time.deltaTime;

        if (t < 0) t = notes.Count - 1 + t;

        if (useLooks && notes[(int)t].useLooksHandle)
            dir = notes[(int)t].lookHandle - transform.position;

        if (dir != Vector3.zero)
        {
            Quaternion q;

            q = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, q, Time.deltaTime * angulerSpeed * notes[(int)t].looksPower);
        }
    }

}

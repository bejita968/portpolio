using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Playables;

public class Procedure_Origin : MonoBehaviour
{
    public enum ProcedureType {
        None, 
        ObjectClick, // Object�� �������� Ŭ�� // Object�� Collider, EventTrigger �߰��Ǿ� �־����
        UIConfirm,  // UI Ȯ�� ��ư Ŭ��
        Timeline,  // Ÿ�Ӷ��� ���
        Narration,  // �����̼Ǹ� ���
        ObjectToObject // ��� ������Ʈ(����)�� �տ� ��� Ÿ�� �������� ���ٴ��
    }

    [Header("Procedure Settings")]
    public ProcedureType procedureType; 
    public int position;

    [Header("Help Text Settings")]
    public bool useHelp;        // �ȳ� UI�� �� ���ΰ�~?
    public Canvas helpCanvas;   // �ȳ� UI Canvas

    [Header("Object Click Settings")]    
    public GameObject targetObject;      //Ŭ���� ������Ʈ
    public bool doNotHideTargetObject;    // ���ν��� ������ ���
    public bool useHighlight;             // �ƿ����� ��� ����
    public Outline highLightObject;       // �ƿ����� ĥ ������Ʈ // ������Ʈ�� Outline.cs ���ԵǾ����
    public bool replaceTargetObject;      // ������Ʈ ��ü ����
    public GameObject replacedObject;      // ������Ʈ ��ü�� ������Ʈ

    [Header("Object To Object Settings")]
    public GameObject heldObject;       // �տ� �� ������Ʈ (�̸� ���� child�� �ٿ�����, Collider�� �θ� �Ǿ��ִ� ���·�)
    public ObjectToObjectTarget targetPoint;      // heldObject�� ���ٴ���ϴ� �浹 ����Ʈ
    public bool isHeldObjectStay;       // Procedure �ϳ� ������ ��� �ִ� ������Ʈ�� ��� ���� ���� ���̳�

    [Header("UI Confirm Settings")]
    public Canvas targetCanvas;         // Canvas
    public Button confirmButton;        // Ȯ�� ��ư

    [Header("Timeline Settings")]
    public PlayableDirector timeline;   // ����� Timeline ���Ե� ������Ʈ    

    [Header("Narration Settings")]
    public AudioClip narrationClip;     // �����̼� Ŭ��

    // general
    private AudioSource audioSource;    // OVRCameraRig�� �޸� AudioSource

    private void Start()
    {
        audioSource = Camera.main.transform.parent.parent.GetComponent<AudioSource>();
    }

    public void ProgressProcedure()
    {
        audioSource.Stop();

        if (narrationClip) {
            audioSource.clip = narrationClip;
            audioSource.Play();
        }

        if (useHelp) {
            helpCanvas.gameObject.SetActive(true);
        }

        if (procedureType == ProcedureType.ObjectClick) {
            EventTrigger trigger = targetObject.GetComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener((eventData) =>
                XR_SceneManager.Instance.InitializeMainTraining());
            
            trigger.triggers.Add(entry);

            if(useHighlight)
            {
                highLightObject.enabled = true;
            }

            targetObject.SetActive(true);
            targetObject.GetComponent<Collider>().enabled = true;
        }
        
        if (procedureType == ProcedureType.ObjectToObject) {
            heldObject.SetActive(true);
            targetPoint.gameObject.SetActive(true);
            targetPoint.InitializeTargetPoint();
            
            if (useHighlight)
            {
                highLightObject.enabled = true;
            }
        }

        if (procedureType == ProcedureType.UIConfirm) {
            confirmButton.onClick.AddListener(XR_SceneManager.Instance.InitializeMainTraining);
            targetCanvas.gameObject.SetActive(true);
        }
        
        if (procedureType == ProcedureType.Timeline) {
            timeline.gameObject.SetActive(true);
            timeline.Play();
            StartCoroutine(Co_EndProcedureAfterTime((float)timeline.playableAsset.duration));
        }

        if (procedureType == ProcedureType.Narration) {
            StartCoroutine(Co_EndProcedureAfterTime(narrationClip.length));
        }
    }

    private IEnumerator Co_EndProcedureAfterTime(float time)
    {        
        yield return new WaitForSeconds(time + 0.3f);

        XR_SceneManager.Instance.InitializeMainTraining();
    }

    public void CleanUpProcedure()
    {        
        audioSource.Stop();

        if(targetObject) {
            targetObject.GetComponent<Collider>().enabled = false;
            targetObject.SetActive(false);
        }

        if(targetPoint) {
            targetPoint.GetComponent<Collider>().enabled = false;
            targetPoint.gameObject.SetActive(false);
        }

        if(replaceTargetObject) {
            replacedObject.SetActive(true);
        }

        if(heldObject && !isHeldObjectStay) {
            heldObject.SetActive(false);
        }

        if(useHelp) {
            helpCanvas.gameObject.SetActive(false);
        }

        if(useHighlight) {
            highLightObject.enabled = false;
        }

        if(targetCanvas) {
            targetCanvas.gameObject.SetActive(false);
        }

        if(timeline) {
            timeline.Stop();
            timeline.gameObject.SetActive(false); // ��
        }        
    }
    
    public void WaitingMode()
    {
        if (targetObject) {
            if(!doNotHideTargetObject) {
                targetObject.SetActive(false);
            }
            targetObject.GetComponent<Collider>().enabled = false;
        }

        if (useHelp) {
            helpCanvas.gameObject.SetActive(false);
        }

        if (highLightObject) {
            highLightObject.enabled = false;
        }

        if(targetPoint) {
            targetPoint.GetComponent<Collider>().enabled = false;
            targetPoint.gameObject.SetActive(false);
        }

        if(replacedObject) {
            replacedObject.SetActive(false);
        }

        if (targetCanvas) {
            targetCanvas.gameObject.SetActive(false);
        }

        if (timeline) {
            timeline.Stop();
            timeline.gameObject.SetActive(false); // ��
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

public class GameMenuController : MonoBehaviour
{
    public GameObject gameMenu;
    public bool isMenuActive = false;//��ʼʱ�رղ˵�

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameMenu.SetActive(isMenuActive); //ȷ���˵��ڿ�ʼʱ�ر�
    }

    // Update is called once per frame
    void Update()
    {
        Keyboard keyboard = Keyboard.current;//��ȡ������������
        if(keyboard == null) return;

        if(keyboard.escapeKey.wasPressedThisFrame)
        {  
             if (PauseMenu.Instance != null && PauseMenu.Instance.OnEscape()) return;
            if(isMenuActive)
            {
                ResumeGame(); //����˵��Ѿ��������ESC����رղ˵����ָ���Ϸ
                return;
            }
            isMenuActive = !isMenuActive; //�л��˵��ļ���״̬
            gameMenu.SetActive(isMenuActive);
            Time.timeScale = 0;//��ͣ��Ϸ
        }
    }

    public void ResumeGame()
    {
        isMenuActive = false;
        gameMenu.SetActive(isMenuActive); //�رղ˵�
        Time.timeScale = 1;//�ָ���Ϸ
    }
}

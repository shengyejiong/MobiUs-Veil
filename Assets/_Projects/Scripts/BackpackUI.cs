using TMPro;
using UnityEngine;

public class BackpackUI : MonoBehaviour
{
    [SerializeField] private GameObject backpackPanel;
    [SerializeField] private TMP_Text itemListText;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        IsOpen = false;//最开始隐藏背包
        backpackPanel.SetActive(false);
    }

    [ContextMenu("测试/关闭背包")] 
    public void Open()
    {
        
        if (IsOpen) return;
        Refresh();
        IsOpen = true;
        backpackPanel.SetActive(true);
        backpackPanel.transform.SetAsLastSibling();
        Time.timeScale = 0f;//暂停游戏
    }

    [ContextMenu("测试/打开背包")]
    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        backpackPanel.SetActive(false);
        Time.timeScale = 1f;//恢复游戏
    }

    private void Refresh()
    {
        itemListText.text = string.Empty;//先清空用来刷新状态
        foreach (StoryItemId itemId in System.Enum.GetValues(typeof(StoryItemId)))
        {
            if(GameProgress.GetState(itemId)!= StoryItemState.InBag)
            {
                continue;
            }
            switch (itemId)
            {
                case StoryItemId.MessageCard:
                    itemListText.text += "留言卡\n";
                    break;
                case StoryItemId.MemorialPlaque:
                    itemListText.text += "纪念牌\n";
                    break;
                case StoryItemId.Clock:
                    itemListText.text += "时钟\n";
                    break;
                default:
                    break;
            }
        }
        if(string.IsNullOrEmpty(itemListText.text))
        {
            itemListText.text = "背包是空的";
        }
    }
}

using UnityEngine;

public sealed class TitleFileSelectPanel : MonoBehaviour
{
    [SerializeField] private TitleSaveSlotCard[] slotCards;

    private TitleScreenController controller;
    private SaveSlotData[] cachedSlots;

    private void Awake()
    {
        EnsureSlotCards();
        AssignOwnerToCards();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void OnValidate()
    {
        EnsureSlotCards();
    }

    public void SetController(TitleScreenController titleController)
    {
        controller = titleController;
        AssignOwnerToCards();
    }

    public void Refresh()
    {
        if (!Game.IsReady || Game.Ctx?.Saves == null)
            return;

        EnsureSlotCards();

        cachedSlots = Game.Ctx.Saves.ReadAllSlots();

        for (int i = 0; i < slotCards.Length; i++)
        {
            TitleSaveSlotCard card = slotCards[i];
            if (card == null)
                continue;

            SaveSlotData data = i < cachedSlots.Length
                ? cachedSlots[i]
                : SaveSlotData.CreateEmpty(i);

            card.SetOwner(this);
            card.Bind(data);
        }
    }

    public void UseSlot(int slotIndex)
    {
        controller?.UseSaveSlot(slotIndex);
    }

    public void UseSlot(SaveSlotData data)
    {
        controller?.UseSaveSlot(data);
    }

    public void DeleteSlot(int slotIndex)
    {
        controller?.DeleteSaveSlot(slotIndex);
    }

    public bool TryGetCachedSlot(int slotIndex, out SaveSlotData data)
    {
        data = null;

        if (cachedSlots == null || slotIndex < 0 || slotIndex >= cachedSlots.Length)
            return false;

        data = cachedSlots[slotIndex];
        return data != null;
    }

    private void AssignOwnerToCards()
    {
        if (slotCards == null)
            return;

        foreach (TitleSaveSlotCard card in slotCards)
        {
            if (card != null)
                card.SetOwner(this);
        }
    }

    private void EnsureSlotCards()
    {
        if (HasCompleteSlotCardList())
            return;

        slotCards = GetComponentsInChildren<TitleSaveSlotCard>(true);
    }

    private bool HasCompleteSlotCardList()
    {
        if (slotCards == null || slotCards.Length < SaveSystem.SlotCount)
            return false;

        for (int i = 0; i < SaveSystem.SlotCount; i++)
        {
            if (slotCards[i] == null)
                return false;
        }

        return true;
    }
}

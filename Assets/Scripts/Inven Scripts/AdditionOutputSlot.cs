using UnityEngine;
using UnityEngine.Events;

public class AdditionOutputSlot : MonoBehaviour, IInteractable
{
    [SerializeField] private AdditionMachine machine;

    [Header("Rules")]
    [SerializeField] private bool clearInputsAfterSuccess = true;

    public AdditionMachine Machine => machine;

    [Header("Events")]
    [SerializeField] private UnityEvent onOutputSlotChanged;

    public void Interact(GameObject interactor)
    {
        if (!Game.IsReady || Game.Ctx.Inventory == null || Game.Ctx.ItemDb == null)
            return;

        if (machine == null)
        {
            Debug.LogError($"{name}: Output slot missing machine reference.");
            return;
        }

        Debug.Log($"[{name}] OutputSlot using machine '{machine.name}' operation={machine.OperationName}");

        if (!machine.HasBothInputs)
        {
            Debug.Log($"{name}: Need both inputs filled.");
            return;
        }

        int result = machine.Result;

        // If result is 0 or negative, output stays invalid and cannot be taken.
        if (result <= 0)
        {
            Debug.Log($"{name}: Output invalid (result={result}).");
            onOutputSlotChanged?.Invoke(); // lets your visuals remain/refresh as invalid if you’re using this event
            return;
        }

        // Convert result -> a hardworm pack definition
        var outDef = Game.Ctx.ItemDb.GetHardwormByPackSize(result);
        if (outDef == null)
        {
            Debug.Log($"{name}: No hardworm pack definition exists for result={result}.");
            return;
        }

        bool added = Game.Ctx.Inventory.TryAdd(outDef.itemId, 1);
        if (!added)
        {
            Debug.Log($"{name}: Backpack is full (4 types). Cannot add result '{outDef.itemId}'.");
            return;
        }

        var cadenceSfx = machine.GetComponent<AdditionMachineCadenceSfx>();
        cadenceSfx?.PlayOutputPickup(result);
        
        var cadenceSfxSub = machine.GetComponent<SubtractionMachineCadenceSfx>();
        cadenceSfxSub?.PlayOutputPickup(result);

        if (clearInputsAfterSuccess)
            machine.ClearInputs();

        onOutputSlotChanged?.Invoke();

        Debug.Log($"{machine.OperationName} result: {result} hardworms -> +1 {outDef.displayName}");
    }
}

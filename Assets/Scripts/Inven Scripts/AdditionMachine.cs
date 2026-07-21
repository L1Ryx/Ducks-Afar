using UnityEngine;

public class AdditionMachine : MonoBehaviour
{
    public enum MathOperation
    {
        Addition = 0,
        Subtraction = 1,
        Multiplication = 2,
    }

    [Header("Slots")]
    [SerializeField] private AdditionSlot slotA;
    [SerializeField] private AdditionSlot slotB;

    [Header("Mode")]
    [SerializeField] private MathOperation operation = MathOperation.Addition;

    [HideInInspector]
    [SerializeField] private bool isSubtractionMachine = false;

    [HideInInspector]
    [SerializeField] private bool isAdditionMachine = true;

    public bool HasBothInputs => slotA != null && slotB != null && slotA.HasValue && slotB.HasValue;

    public MathOperation Operation => ResolveOperation();
    public bool IsSubtractionMachine => Operation == MathOperation.Subtraction;
    public bool IsAdditionMachine => Operation == MathOperation.Addition;
    public bool IsMultiplicationMachine => Operation == MathOperation.Multiplication;
    public string OperationName => Operation.ToString();

    public int Sum => (slotA != null ? slotA.Value : 0) + (slotB != null ? slotB.Value : 0);

    public int Result
    {
        get
        {
            int a = (slotA != null ? slotA.Value : 0);
            int b = (slotB != null ? slotB.Value : 0);
            MathOperation resolvedOperation = Operation;
            int result = resolvedOperation switch
            {
                MathOperation.Subtraction => a - b,
                MathOperation.Multiplication => a * b,
                _ => a + b,
            };

            Debug.Log($"[{name}] operation={resolvedOperation} a={a} b={b} => {result}");
            return result;
        }
    }

    public bool HasValidResult => HasBothInputs && Result > 0;

    public void ClearInputs()
    {
        slotA?.ClearNoRefund();
        slotB?.ClearNoRefund();
    }

    public bool WereBothInputsEmptyBeforePlacing(AdditionSlot placingSlot)
    {
        // Called from AdditionSlot BEFORE it sets HasValue=true.
        // If the "other" slot is empty right now, then both were empty.
        if (slotA == null || slotB == null) return true;

        if (placingSlot == slotA)
            return !slotB.HasValue;

        if (placingSlot == slotB)
            return !slotA.HasValue;

        // If something unexpected calls this, treat as first placement.
        return true;
    }

    private void OnValidate()
    {
        if (operation == MathOperation.Addition && isSubtractionMachine)
            operation = MathOperation.Subtraction;

        isSubtractionMachine = operation == MathOperation.Subtraction;
        isAdditionMachine = operation == MathOperation.Addition;
    }

    private MathOperation ResolveOperation()
    {
        if (operation == MathOperation.Addition && isSubtractionMachine)
            return MathOperation.Subtraction;

        return operation;
    }
}

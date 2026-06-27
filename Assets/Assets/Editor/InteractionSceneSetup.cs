#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class InteractionSceneSetup
{
    private const string InteractionCursorName = "InteractionCursor";
    private const string TrashCanName = "TrashCan";
    private const string InteractZoneName = "InteractZone";

    private static readonly Vector3 BowlZoneCenter = new Vector3(0f, 0.35f, 0f);
    private static readonly Vector3 BowlZoneSize = new Vector3(1.35f, 0.9f, 1.35f);
    private static readonly Vector3 TrashZoneCenter = new Vector3(0f, 0.35f, 0f);
    private static readonly Vector3 TrashZoneSize = new Vector3(1.05f, 0.9f, 1.05f);
    private static readonly Vector3 TrashCanPosition = new Vector3(2f, 0.35f, 0f);

    static InteractionSceneSetup()
    {
        EditorApplication.delayCall += EnsureTrashCanInOpenScene;
    }

    private static void EnsureTrashCanInOpenScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (GameObject.Find(TrashCanName) != null)
            return;

        if (Object.FindObjectOfType<Kase>() == null)
            return;

        PlayerInventory inventory = Object.FindObjectOfType<PlayerInventory>();
        EnsureTrashCan(inventory);

        Scene scene = SceneManager.GetActiveScene();
        if (scene.IsValid())
            EditorSceneManager.MarkSceneDirty(scene);
    }

    [MenuItem("Tools/SummerJam/Setup Interaction Zones")]
    [MenuItem("SummerJam/Setup Interaction Zones")]
    public static void SetupInteractionZones()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogWarning("InteractionSceneSetup: Aktif sahne bulunamadi.");
            return;
        }

        PlayerInventory inventory = Object.FindObjectOfType<PlayerInventory>();
        EnsureInteractionCursor();
        EnsureBowlInteractZone(inventory);
        EnsureTrashCan(inventory);
        EnsureInputHandler();

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("InteractionSceneSetup: Kase ve cop trigger alanlari guncellendi.");
    }

    private static void EnsureInteractionCursor()
    {
        GameObject cursor = GameObject.Find(InteractionCursorName);
        if (cursor == null)
        {
            cursor = new GameObject(InteractionCursorName);
            Undo.RegisterCreatedObjectUndo(cursor, "Create Interaction Cursor");
        }

        SphereCollider sphereCollider = cursor.GetComponent<SphereCollider>();
        if (sphereCollider != null)
            Undo.DestroyObjectImmediate(sphereCollider);

        Rigidbody rigidbody = cursor.GetComponent<Rigidbody>();
        if (rigidbody != null)
            Undo.DestroyObjectImmediate(rigidbody);

        if (cursor.GetComponent<PlayerInteractionBody>() == null)
            Undo.AddComponent<PlayerInteractionBody>(cursor);

        cursor.transform.position = new Vector3(0f, 0.5f, 0f);
    }

    private static void EnsureBowlInteractZone(PlayerInventory inventory)
    {
        Kase kase = Object.FindObjectOfType<Kase>();
        if (kase == null)
        {
            Debug.LogWarning("InteractionSceneSetup: Kase bulunamadi.");
            return;
        }

        EnsureInteractZoneChild(kase.transform, BowlZoneCenter, BowlZoneSize);

        BowlDeposit bowlDeposit = kase.GetComponent<BowlDeposit>();
        if (bowlDeposit == null)
            bowlDeposit = Undo.AddComponent<BowlDeposit>(kase.gameObject);

        SerializedObject serializedBowlDeposit = new SerializedObject(bowlDeposit);
        serializedBowlDeposit.FindProperty("kase").objectReferenceValue = kase;
        serializedBowlDeposit.FindProperty("inventory").objectReferenceValue = inventory;
        serializedBowlDeposit.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureTrashCan(PlayerInventory inventory)
    {
        GameObject trashCan = GameObject.Find(TrashCanName);
        if (trashCan == null)
        {
            trashCan = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trashCan.name = TrashCanName;
            Undo.RegisterCreatedObjectUndo(trashCan, "Create Trash Can");

            Object.DestroyImmediate(trashCan.GetComponent<BoxCollider>());
            trashCan.transform.position = TrashCanPosition;
            trashCan.transform.localScale = new Vector3(0.6f, 0.7f, 0.6f);
        }

        EnsureInteractZoneChild(trashCan.transform, TrashZoneCenter, TrashZoneSize);

        TrashBin trashBin = trashCan.GetComponent<TrashBin>();
        if (trashBin == null)
            trashBin = Undo.AddComponent<TrashBin>(trashCan);

        SerializedObject serializedTrashBin = new SerializedObject(trashBin);
        serializedTrashBin.FindProperty("inventory").objectReferenceValue = inventory;
        serializedTrashBin.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureInputHandler()
    {
        if (Object.FindObjectOfType<InteractionInputHandler>() != null)
            return;

        GameObject handlerObject = new GameObject("InteractionInputHandler");
        Undo.RegisterCreatedObjectUndo(handlerObject, "Create Interaction Input Handler");
        Undo.AddComponent<InteractionInputHandler>(handlerObject);
    }

    private static InteractionTriggerZone EnsureInteractZoneChild(
        Transform parent,
        Vector3 localCenter,
        Vector3 size)
    {
        Transform zoneTransform = parent.Find(InteractZoneName);
        GameObject zoneObject;

        if (zoneTransform == null)
        {
            zoneObject = new GameObject(InteractZoneName);
            Undo.RegisterCreatedObjectUndo(zoneObject, "Create Interact Zone");
            zoneObject.transform.SetParent(parent, false);
        }
        else
        {
            zoneObject = zoneTransform.gameObject;
        }

        zoneObject.transform.localPosition = localCenter;
        zoneObject.transform.localRotation = Quaternion.identity;
        zoneObject.transform.localScale = Vector3.one;

        BoxCollider boxCollider = zoneObject.GetComponent<BoxCollider>();
        if (boxCollider == null)
            boxCollider = Undo.AddComponent<BoxCollider>(zoneObject);

        boxCollider.isTrigger = true;
        boxCollider.center = Vector3.zero;
        boxCollider.size = size;

        InteractionTriggerZone zone = zoneObject.GetComponent<InteractionTriggerZone>();
        if (zone == null)
            zone = Undo.AddComponent<InteractionTriggerZone>(zoneObject);

        SerializedObject serializedZone = new SerializedObject(zone);
        serializedZone.FindProperty("zoneCollider").objectReferenceValue = boxCollider;
        serializedZone.ApplyModifiedPropertiesWithoutUndo();

        return zone;
    }
}
#endif

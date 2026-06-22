using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class CameraRoomAStarChaser2D : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform target;
    [SerializeField] private GameplayCameraRoom room;
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private bool autoFindTarget = true;
    [SerializeField] private bool autoFindRoom = true;

    [Header("Room Rules")]
    [SerializeField] private bool onlyChaseWhenTargetInRoom = true;
    [SerializeField] private bool stopWhenOutsideRoom = true;
    [SerializeField] private bool clampToRoomBounds = true;
    [SerializeField, Min(0f)] private float roomEdgePadding = 0.15f;

    [Header("Detection")]
    [SerializeField] private bool useDetectionRange = true;
    [SerializeField, Min(0f)] private float detectionRange = 6f;

    [Header("Home")]
    [SerializeField] private bool returnHomeWhenTargetLeavesRoom = true;
    [SerializeField] private bool cacheHomePositionOnAwake = true;
    [SerializeField, Min(0f)] private float homeStoppingDistance = 0.08f;
    [SerializeField] private Vector2 homePosition;

    [Header("Flee")]
    [SerializeField] private bool fleeFromTarget;
    [SerializeField, Min(0.25f)] private float fleeDistance = 4f;
    [SerializeField, Min(0f)] private float fleeStoppingDistance = 0.15f;

    [Header("Pathfinding")]
    [SerializeField] private LayerMask obstacleMask = Physics2D.DefaultRaycastLayers;
    [SerializeField, Min(0.05f)] private float cellSize = 0.5f;
    [SerializeField, Min(0f)] private float agentRadius = 0.25f;
    [SerializeField] private bool sizeAgentFromCollider = true;
    [SerializeField] private bool useColliderBoundingCircle = true;
    [SerializeField] private bool useColliderShapeForClearance = true;
    [SerializeField, Min(0f)] private float colliderClearancePadding = 0.05f;
    [SerializeField] private bool includeTriggerObstacles;
    [SerializeField] private bool allowDiagonalMovement;
    [SerializeField] private bool preventCornerCutting = true;
    [SerializeField] private bool validateMovementBetweenCells = true;
    [SerializeField, Min(16)] private int maxVisitedNodes = 2500;
    [SerializeField, Min(0)] private int nearestWalkableSearchRadius = 6;

    [Header("Path Refresh")]
    [SerializeField, Min(0.02f)] private float repathInterval = 0.2f;
    [SerializeField, Min(0f)] private float targetMoveRepathDistance = 0.35f;
    [SerializeField, Min(0f)] private float waypointReachDistance = 0.12f;
    [SerializeField, Min(0f)] private float passedWaypointAdvanceDistance = 0.3f;
    [SerializeField] private bool useLineOfSightShortcuts = true;
    [SerializeField, Min(0)] private int shortcutLookAheadNodes = 6;
    [SerializeField, Min(0f)] private float stoppingDistance = 0.4f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float maxSpeed = 2.2f;
    [SerializeField, Min(0f)] private float acceleration = 18f;
    [SerializeField, Min(0f)] private float deceleration = 24f;
    [SerializeField, Range(0f, 1f)] private float cornerSlowdown = 0.25f;
    [SerializeField] private bool slideAlongObstacles = true;
    [SerializeField, Min(0f)] private float obstacleSlideSkinWidth = 0.03f;
    [SerializeField, Range(1, 4)] private int obstacleSlideIterations = 2;
    [SerializeField, Min(0f)] private float minSlideSpeed = 0.05f;
    [SerializeField] private bool faceMovementDirection;
    [SerializeField, Min(0f)] private float facingFlipDeadzone = 0.03f;

    [Header("Natural Movement")]
    [SerializeField] private bool useSpeedNoise = true;
    [SerializeField, Range(0f, 0.6f)] private float speedNoiseAmount = 0.14f;
    [SerializeField, Min(0.01f)] private float speedNoiseFrequency = 0.8f;

    [Header("Stuck Recovery")]
    [SerializeField] private bool useStuckRecovery = true;
    [SerializeField, Min(0.05f)] private float stuckCheckInterval = 0.35f;
    [SerializeField, Min(0f)] private float stuckMinMoveDistance = 0.06f;
    [SerializeField, Min(0)] private int stuckRepathsBeforeWaypointSkip = 2;
    [SerializeField, Min(0f)] private float blockedWaypointSearchRadius = 1f;

    [Header("Debug")]
    [SerializeField] private bool drawRoomGizmos = true;
    [SerializeField] private bool drawPathGizmos = true;
    [SerializeField] private bool drawSampleGizmos;

    private readonly List<Vector2> path = new List<Vector2>();
    private readonly Collider2D[] overlapResults = new Collider2D[16];
    private readonly RaycastHit2D[] castResults = new RaycastHit2D[16];
    private readonly HashSet<Collider2D> ignoredColliders = new HashSet<Collider2D>();

    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private Collider2D movementCollider;
    private ContactFilter2D obstacleFilter;
    private float nextRepathTime;
    private int waypointIndex;
    private Vector2 lastTargetPathPosition;
    private bool hasLastTargetPathPosition;
    private Vector2 lastStuckCheckPosition;
    private bool hasLastStuckCheckPosition;
    private float nextStuckCheckTime;
    private int consecutiveStuckRepaths;
    private bool hasHomePosition;
    private bool wasReturningHome;
    private float speedNoiseSeed;

    private static readonly Vector2Int[] FourWay =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
    };

    private static readonly Vector2Int[] EightWay =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
        new Vector2Int(1, 1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, 1),
        new Vector2Int(-1, -1),
    };

    public GameplayCameraRoom Room => room;

    private void Reset()
    {
        body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.gravityScale = 0f;
            body.freezeRotation = true;
        }
    }

    private void Awake()
    {
        EnsureRefs();
        ConfigurePhysics();

        if (cacheHomePositionOnAwake)
            CacheHomePosition();

        speedNoiseSeed = Random.value * 1000f;
    }

    private void OnEnable()
    {
        nextRepathTime = 0f;
        hasLastTargetPathPosition = false;
    }

    private void OnValidate()
    {
        if (cellSize < 0.05f)
            cellSize = 0.05f;

        if (repathInterval < 0.02f)
            repathInterval = 0.02f;

        if (stuckCheckInterval < 0.05f)
            stuckCheckInterval = 0.05f;

        if (maxVisitedNodes < 16)
            maxVisitedNodes = 16;

        if (speedNoiseFrequency < 0.01f)
            speedNoiseFrequency = 0.01f;

        if (body == null)
            body = GetComponent<Rigidbody2D>();

        if (body != null)
        {
            body.gravityScale = 0f;
            body.freezeRotation = true;
        }
    }

    private void FixedUpdate()
    {
        EnsureRefs();

        if (body == null || room == null)
        {
            Stop();
            return;
        }

        Vector2 currentPosition = body.position;
        bool targetAvailable = target != null;
        bool selfInRoom = room.Contains(currentPosition);
        bool targetInRoom = targetAvailable && room.Contains(target.position);
        bool targetInDetectionRange = targetAvailable && IsTargetInDetectionRange(currentPosition);
        bool blockedByRoomRule = onlyChaseWhenTargetInRoom && !targetInRoom;
        bool blockedByDetection = !targetInDetectionRange;
        bool targetCanBeChased = targetAvailable && !blockedByRoomRule && !blockedByDetection;

        if (stopWhenOutsideRoom && !selfInRoom)
        {
            ClearPath();
            Stop();
            ClampBodyToRoom();
            return;
        }

        bool shouldReturnHome = returnHomeWhenTargetLeavesRoom &&
                                !targetCanBeChased &&
                                hasHomePosition;

        if (!targetCanBeChased && !shouldReturnHome)
        {
            ClearPath();
            Stop();
            ClampBodyToRoom();
            return;
        }

        bool shouldFlee = !shouldReturnHome && fleeFromTarget && targetCanBeChased;
        Vector2 goalPosition = shouldReturnHome
            ? homePosition
            : shouldFlee
                ? GetFleeGoalPosition(currentPosition, target.position)
                : (Vector2)target.position;
        float activeStoppingDistance = shouldReturnHome
            ? homeStoppingDistance
            : shouldFlee
                ? fleeStoppingDistance
                : stoppingDistance;

        if (shouldReturnHome != wasReturningHome)
        {
            ClearPath();
            nextRepathTime = 0f;
            wasReturningHome = shouldReturnHome;
        }

        if ((goalPosition - currentPosition).sqrMagnitude <= activeStoppingDistance * activeStoppingDistance)
        {
            ClearPath();
            Stop();
            ClampBodyToRoom();
            return;
        }

        if (ShouldRepath(goalPosition))
            Repath(currentPosition, goalPosition);

        ShortcutVisibleWaypoints(currentPosition, goalPosition);
        FollowPath(goalPosition);
        CheckStuck(currentPosition, goalPosition);
        ClampBodyToRoom();
    }

    private void EnsureRefs()
    {
        if (body == null)
            body = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (movementCollider == null || movementCollider.isTrigger)
            movementCollider = FindMovementCollider();

        if (autoFindTarget && target == null)
        {
            GameObject targetObject = GameObject.FindWithTag(targetTag);
            if (targetObject != null)
                target = targetObject.transform;
        }

        if (autoFindRoom && room == null)
            room = FindContainingRoom(transform.position);

        CacheIgnoredColliders();
    }

    private void ConfigurePhysics()
    {
        obstacleFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = obstacleMask,
            useTriggers = includeTriggerObstacles,
        };
    }

    public void CacheHomePosition()
    {
        EnsureRefs();
        homePosition = body != null ? body.position : (Vector2)transform.position;
        hasHomePosition = true;
    }

    public void SetFleeFromTarget(bool shouldFlee)
    {
        if (fleeFromTarget == shouldFlee)
            return;

        fleeFromTarget = shouldFlee;
        ClearPath();
        nextRepathTime = 0f;
    }

    public void FreezeMovement()
    {
        ClearPath();

        if (body == null)
            body = GetComponent<Rigidbody2D>();

        if (body != null)
            body.linearVelocity = Vector2.zero;

        enabled = false;
    }

    private void CacheIgnoredColliders()
    {
        ignoredColliders.Clear();

        foreach (Collider2D ownCollider in GetComponentsInChildren<Collider2D>())
        {
            if (ownCollider != null)
                ignoredColliders.Add(ownCollider);
        }

        if (target == null)
            return;

        foreach (Collider2D targetCollider in target.GetComponentsInChildren<Collider2D>())
        {
            if (targetCollider != null)
                ignoredColliders.Add(targetCollider);
        }
    }

    private Collider2D FindMovementCollider()
    {
        Collider2D fallback = null;
        foreach (Collider2D candidate in GetComponentsInChildren<Collider2D>())
        {
            if (candidate == null)
                continue;

            if (fallback == null)
                fallback = candidate;

            if (!candidate.isTrigger)
                return candidate;
        }

        return fallback;
    }

    private GameplayCameraRoom FindContainingRoom(Vector2 worldPosition)
    {
        GameplayCameraRoom[] rooms = Object.FindObjectsByType<GameplayCameraRoom>(
            FindObjectsInactive.Include);

        GameplayCameraRoom nearest = null;
        float nearestDistance = float.PositiveInfinity;

        for (int i = 0; i < rooms.Length; i++)
        {
            GameplayCameraRoom candidate = rooms[i];
            if (candidate == null || candidate.gameObject.scene != gameObject.scene)
                continue;

            if (candidate.Contains(worldPosition))
                return candidate;

            float distance = candidate.DistanceSquaredTo(worldPosition);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = candidate;
            }
        }

        return nearest;
    }

    private bool ShouldRepath(Vector2 targetPosition)
    {
        if (Time.time >= nextRepathTime)
            return true;

        if (!hasLastTargetPathPosition)
            return true;

        return (targetPosition - lastTargetPathPosition).sqrMagnitude >=
               targetMoveRepathDistance * targetMoveRepathDistance;
    }

    private Vector2 GetFleeGoalPosition(Vector2 currentPosition, Vector2 threatPosition)
    {
        Bounds bounds = GetPaddedRoomBounds();
        Vector2 away = currentPosition - threatPosition;
        if (away.sqrMagnitude <= 0.0001f)
            away = hasHomePosition ? currentPosition - homePosition : Vector2.up;

        away.Normalize();

        Vector2 right = new Vector2(away.y, -away.x);
        Vector2[] directions =
        {
            away,
            (away + right * 0.65f).normalized,
            (away - right * 0.65f).normalized,
            right,
            -right,
            -away,
        };

        Vector2 best = currentPosition;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < directions.Length; i++)
        {
            Vector2 candidate = ClampToBounds(currentPosition + directions[i] * fleeDistance, bounds);
            if (IsBlocked(candidate))
                continue;

            float distanceFromThreat = (candidate - threatPosition).sqrMagnitude;
            float distanceFromCurrent = (candidate - currentPosition).sqrMagnitude;
            float score = distanceFromThreat + distanceFromCurrent * 0.2f;
            if (score <= bestScore)
                continue;

            bestScore = score;
            best = candidate;
        }

        if (bestScore > float.NegativeInfinity)
            return best;

        return ClampToBounds(currentPosition + away * Mathf.Min(fleeDistance, 1f), bounds);
    }

    private void Repath(Vector2 start, Vector2 goal)
    {
        nextRepathTime = Time.time + repathInterval;
        lastTargetPathPosition = goal;
        hasLastTargetPathPosition = true;

        if (TryFindPath(start, goal, path))
        {
            waypointIndex = 0;
            consecutiveStuckRepaths = 0;
        }
        else
        {
            ClearPath();
        }
    }

    private bool TryFindPath(Vector2 startWorld, Vector2 goalWorld, List<Vector2> result)
    {
        result.Clear();

        Bounds bounds = GetPaddedRoomBounds();
        GridInfo grid = new GridInfo(bounds, cellSize);

        if (grid.Width <= 0 || grid.Height <= 0 || grid.CellCount > maxVisitedNodes)
            return false;

        Vector2Int startCell = grid.WorldToCell(ClampToBounds(startWorld, bounds));
        Vector2Int goalCell = grid.WorldToCell(ClampToBounds(goalWorld, bounds));

        if (!TryFindNearestWalkable(grid, startCell, out startCell))
            return false;

        if (!TryFindNearestWalkable(grid, goalCell, out goalCell))
            return false;

        int cellCount = grid.CellCount;
        bool[] closed = new bool[cellCount];
        bool[] opened = new bool[cellCount];
        float[] gScore = new float[cellCount];
        Vector2Int[] cameFrom = new Vector2Int[cellCount];
        List<Vector2Int> open = new List<Vector2Int>(cellCount);

        for (int i = 0; i < cellCount; i++)
        {
            gScore[i] = float.PositiveInfinity;
            cameFrom[i] = new Vector2Int(int.MinValue, int.MinValue);
        }

        int startIndex = grid.Index(startCell);
        gScore[startIndex] = 0f;
        opened[startIndex] = true;
        open.Add(startCell);

        int visited = 0;
        while (open.Count > 0 && visited < maxVisitedNodes)
        {
            Vector2Int current = PopBestOpenCell(open, grid, gScore, goalCell);
            int currentIndex = grid.Index(current);
            opened[currentIndex] = false;
            closed[currentIndex] = true;
            visited++;

            if (current == goalCell)
            {
                BuildPath(grid, current, startCell, cameFrom, result);
                SmoothPath(result);
                return result.Count > 0;
            }

            Vector2Int[] directions = allowDiagonalMovement ? EightWay : FourWay;
            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int neighbor = current + directions[i];
                if (!grid.Contains(neighbor))
                    continue;

                int neighborIndex = grid.Index(neighbor);
                if (closed[neighborIndex] || !IsWalkable(grid, neighbor))
                    continue;

                if (validateMovementBetweenCells &&
                    !CanMoveBetweenCells(grid, current, neighbor))
                {
                    continue;
                }

                bool diagonal = directions[i].x != 0 && directions[i].y != 0;
                if (diagonal && preventCornerCutting && IsDiagonalCornerCut(grid, current, directions[i]))
                    continue;

                float moveCost = diagonal ? 1.4142135f : 1f;
                float tentativeG = gScore[currentIndex] + moveCost;
                if (tentativeG >= gScore[neighborIndex])
                    continue;

                cameFrom[neighborIndex] = current;
                gScore[neighborIndex] = tentativeG;

                if (!opened[neighborIndex])
                {
                    opened[neighborIndex] = true;
                    open.Add(neighbor);
                }
            }
        }

        return false;
    }

    private Vector2Int PopBestOpenCell(List<Vector2Int> open, GridInfo grid, float[] gScore, Vector2Int goal)
    {
        int bestListIndex = 0;
        float bestScore = float.PositiveInfinity;

        for (int i = 0; i < open.Count; i++)
        {
            Vector2Int cell = open[i];
            float score = gScore[grid.Index(cell)] + Heuristic(cell, goal);
            if (score >= bestScore)
                continue;

            bestScore = score;
            bestListIndex = i;
        }

        Vector2Int best = open[bestListIndex];
        int lastIndex = open.Count - 1;
        open[bestListIndex] = open[lastIndex];
        open.RemoveAt(lastIndex);
        return best;
    }

    private void BuildPath(
        GridInfo grid,
        Vector2Int goal,
        Vector2Int start,
        Vector2Int[] cameFrom,
        List<Vector2> result)
    {
        List<Vector2> reversed = new List<Vector2>();
        Vector2Int current = goal;

        while (current != start)
        {
            reversed.Add(grid.CellToWorld(current));
            Vector2Int previous = cameFrom[grid.Index(current)];
            if (previous.x == int.MinValue)
                break;

            current = previous;
        }

        for (int i = reversed.Count - 1; i >= 0; i--)
            result.Add(reversed[i]);
    }

    private bool TryFindNearestWalkable(GridInfo grid, Vector2Int center, out Vector2Int walkable)
    {
        if (grid.Contains(center) && IsWalkable(grid, center))
        {
            walkable = center;
            return true;
        }

        for (int radius = 1; radius <= nearestWalkableSearchRadius; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        continue;

                    Vector2Int candidate = center + new Vector2Int(x, y);
                    if (grid.Contains(candidate) && IsWalkable(grid, candidate))
                    {
                        walkable = candidate;
                        return true;
                    }
                }
            }
        }

        walkable = center;
        return false;
    }

    private bool IsWalkable(GridInfo grid, Vector2Int cell)
    {
        Vector2 world = grid.CellToWorld(cell);
        if (!GetPaddedRoomBounds().Contains(world))
            return false;

        return !IsBlocked(world);
    }

    private bool IsDiagonalCornerCut(GridInfo grid, Vector2Int current, Vector2Int direction)
    {
        Vector2Int horizontal = current + new Vector2Int(direction.x, 0);
        Vector2Int vertical = current + new Vector2Int(0, direction.y);
        return !grid.Contains(horizontal) ||
               !grid.Contains(vertical) ||
               !IsWalkable(grid, horizontal) ||
               !IsWalkable(grid, vertical);
    }

    private bool CanMoveBetweenCells(GridInfo grid, Vector2Int from, Vector2Int to)
    {
        if (!grid.Contains(from) || !grid.Contains(to))
            return false;

        return HasClearSegment(grid.CellToWorld(from), grid.CellToWorld(to));
    }

    private bool IsBlocked(Vector2 worldPosition)
    {
        ConfigurePhysics();

        int count = TryGetBoxCastData(worldPosition, out Vector2 boxCenter, out Vector2 boxSize, out float boxAngle)
            ? Physics2D.OverlapBox(boxCenter, boxSize, boxAngle, obstacleFilter, overlapResults)
            : Physics2D.OverlapCircle(worldPosition, GetEffectiveAgentRadius(), obstacleFilter, overlapResults);

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = overlapResults[i];
            if (hit == null || ignoredColliders.Contains(hit))
                continue;

            return true;
        }

        return false;
    }

    private void FollowPath(Vector2 targetPosition)
    {
        if (path.Count == 0 || waypointIndex >= path.Count)
        {
            Stop();
            return;
        }

        Vector2 position = body.position;
        float reachDistance = GetEffectiveWaypointReachDistance();
        while (waypointIndex < path.Count &&
               (path[waypointIndex] - position).sqrMagnitude <= reachDistance * reachDistance)
        {
            waypointIndex++;
        }

        AdvancePastStaleWaypoint(position);

        if (waypointIndex >= path.Count)
        {
            Stop();
            return;
        }

        Vector2 waypoint = path[waypointIndex];
        if (!HasClearSegment(position, waypoint))
        {
            if (TryAdvanceToReachableWaypoint(position))
            {
                waypoint = path[waypointIndex];
            }
            else
            {
                ForceRepath(position, targetPosition);
                Stop();
                return;
            }
        }

        Vector2 toWaypoint = waypoint - position;
        Vector2 direction = toWaypoint.normalized;
        float speed = maxSpeed * GetNaturalSpeedMultiplier();

        if (cornerSlowdown > 0f && waypointIndex < path.Count - 1)
        {
            Vector2 nextDirection = (path[waypointIndex + 1] - waypoint).normalized;
            float turnAmount = 1f - Mathf.Clamp01(Vector2.Dot(direction, nextDirection));
            speed *= Mathf.Lerp(1f, 1f - cornerSlowdown, turnAmount);
        }

        Vector2 desiredVelocity = direction * speed;
        Vector2 commandedVelocity = Vector2.MoveTowards(
            body.linearVelocity,
            desiredVelocity,
            acceleration * Time.fixedDeltaTime);
        body.linearVelocity = ResolveObstacleSlide(commandedVelocity);

        if (faceMovementDirection && spriteRenderer != null && Mathf.Abs(body.linearVelocity.x) > facingFlipDeadzone)
            spriteRenderer.flipX = body.linearVelocity.x < 0f;
    }

    private Vector2 ResolveObstacleSlide(Vector2 desiredVelocity)
    {
        if (!slideAlongObstacles || desiredVelocity.sqrMagnitude <= 0.0001f)
            return desiredVelocity;

        Vector2 adjustedVelocity = desiredVelocity;
        for (int i = 0; i < obstacleSlideIterations; i++)
        {
            if (!TryFindBlockingHit(adjustedVelocity, out RaycastHit2D hit))
                return adjustedVelocity;

            float intoSurface = Vector2.Dot(adjustedVelocity, hit.normal);
            if (intoSurface >= 0f)
                return adjustedVelocity;

            adjustedVelocity -= hit.normal * intoSurface;
            if (adjustedVelocity.sqrMagnitude <= minSlideSpeed * minSlideSpeed)
                return Vector2.zero;
        }

        return adjustedVelocity;
    }

    private bool TryFindBlockingHit(Vector2 velocity, out RaycastHit2D blockingHit)
    {
        blockingHit = default;

        float speed = velocity.magnitude;
        if (speed <= 0.0001f)
            return false;

        ConfigurePhysics();

        Vector2 direction = velocity / speed;
        float distance = speed * Time.fixedDeltaTime + obstacleSlideSkinWidth;
        int count = TryGetBoxCastData(body.position, out Vector2 boxCenter, out Vector2 boxSize, out float boxAngle)
            ? Physics2D.BoxCast(boxCenter, boxSize, boxAngle, direction, obstacleFilter, castResults, distance)
            : Physics2D.CircleCast(body.position, GetEffectiveAgentRadius(), direction, obstacleFilter, castResults, distance);

        float nearestDistance = float.PositiveInfinity;
        bool foundHit = false;
        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = castResults[i];
            if (hit.collider == null || ignoredColliders.Contains(hit.collider))
                continue;

            if (hit.distance > nearestDistance)
                continue;

            nearestDistance = hit.distance;
            blockingHit = hit;
            foundHit = true;
        }

        return foundHit;
    }

    private void ShortcutVisibleWaypoints(Vector2 position, Vector2 targetPosition)
    {
        if (!useLineOfSightShortcuts || path.Count == 0)
            return;

        if (HasClearSegment(position, targetPosition))
        {
            path.Clear();
            path.Add(targetPosition);
            waypointIndex = 0;
            return;
        }

        int furthest = Mathf.Min(path.Count - 1, waypointIndex + shortcutLookAheadNodes);
        for (int i = furthest; i > waypointIndex; i--)
        {
            if (!HasClearSegment(position, path[i]))
                continue;

            waypointIndex = i;
            return;
        }
    }

    private void SmoothPath(List<Vector2> points)
    {
        if (!useLineOfSightShortcuts || points.Count <= 2)
            return;

        int anchor = 0;
        while (anchor < points.Count - 2)
        {
            int furthestVisible = anchor + 1;
            for (int i = points.Count - 1; i > anchor + 1; i--)
            {
                if (!HasClearSegment(points[anchor], points[i]))
                    continue;

                furthestVisible = i;
                break;
            }

            if (furthestVisible <= anchor + 1)
            {
                anchor++;
                continue;
            }

            points.RemoveRange(anchor + 1, furthestVisible - anchor - 1);
            anchor++;
        }
    }

    private void AdvancePastStaleWaypoint(Vector2 position)
    {
        if (passedWaypointAdvanceDistance <= 0f || waypointIndex <= 0 || waypointIndex >= path.Count)
            return;

        Vector2 previous = path[waypointIndex - 1];
        Vector2 current = path[waypointIndex];
        Vector2 segment = current - previous;
        float segmentLengthSquared = segment.sqrMagnitude;
        if (segmentLengthSquared <= 0.0001f)
            return;

        float progress = Vector2.Dot(position - previous, segment) / segmentLengthSquared;
        if (progress < 1f)
            return;

        float distanceFromLine = DistancePointToSegment(position, previous, current);
        if (distanceFromLine <= passedWaypointAdvanceDistance)
            waypointIndex++;
    }

    private void CheckStuck(Vector2 position, Vector2 targetPosition)
    {
        if (!useStuckRecovery || path.Count == 0 || waypointIndex >= path.Count)
            return;

        if (Time.time < nextStuckCheckTime)
            return;

        nextStuckCheckTime = Time.time + stuckCheckInterval;

        if (!hasLastStuckCheckPosition)
        {
            lastStuckCheckPosition = position;
            hasLastStuckCheckPosition = true;
            return;
        }

        if ((position - lastStuckCheckPosition).sqrMagnitude >= stuckMinMoveDistance * stuckMinMoveDistance)
        {
            lastStuckCheckPosition = position;
            consecutiveStuckRepaths = 0;
            return;
        }

        lastStuckCheckPosition = position;

        consecutiveStuckRepaths++;
        if (consecutiveStuckRepaths > stuckRepathsBeforeWaypointSkip && waypointIndex < path.Count - 1)
        {
            waypointIndex++;
            consecutiveStuckRepaths = 0;
            return;
        }

        ForceRepath(position, targetPosition);
    }

    private void ForceRepath(Vector2 start, Vector2 goal)
    {
        nextRepathTime = Time.time + repathInterval;

        if (TryFindPath(start, goal, path))
            waypointIndex = FindBestReachableWaypoint(start);
        else
            ClearPath();
    }

    private int FindBestReachableWaypoint(Vector2 position)
    {
        if (path.Count == 0)
            return 0;

        int furthest = Mathf.Min(path.Count - 1, shortcutLookAheadNodes);
        for (int i = furthest; i >= 0; i--)
        {
            if (HasClearSegment(position, path[i]))
                return i;
        }

        for (int i = 0; i < path.Count; i++)
        {
            if ((path[i] - position).sqrMagnitude <= blockedWaypointSearchRadius * blockedWaypointSearchRadius)
                return i;
        }

        return 0;
    }

    private bool TryAdvanceToReachableWaypoint(Vector2 position)
    {
        if (path.Count == 0)
            return false;

        int furthest = Mathf.Min(path.Count - 1, waypointIndex + shortcutLookAheadNodes);
        for (int i = furthest; i > waypointIndex; i--)
        {
            if (!HasClearSegment(position, path[i]))
                continue;

            waypointIndex = i;
            consecutiveStuckRepaths = 0;
            return true;
        }

        return false;
    }

    private bool HasClearSegment(Vector2 from, Vector2 to)
    {
        Bounds bounds = GetPaddedRoomBounds();
        if (!bounds.Contains(from) || !bounds.Contains(to))
            return false;

        Vector2 delta = to - from;
        float distance = delta.magnitude;
        if (distance <= 0.0001f)
            return true;

        ConfigurePhysics();

        Vector2 direction = delta / distance;
        int count = TryGetBoxCastData(from, out Vector2 boxCenter, out Vector2 boxSize, out float boxAngle)
            ? Physics2D.BoxCast(boxCenter, boxSize, boxAngle, direction, obstacleFilter, castResults, distance)
            : Physics2D.CircleCast(from, GetEffectiveAgentRadius(), direction, obstacleFilter, castResults, distance);

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = castResults[i].collider;
            if (hit == null || ignoredColliders.Contains(hit))
                continue;

            return false;
        }

        return true;
    }

    private void Stop()
    {
        if (body == null)
            return;

        body.linearVelocity = Vector2.MoveTowards(
            body.linearVelocity,
            Vector2.zero,
            deceleration * Time.fixedDeltaTime);
    }

    private bool IsTargetInDetectionRange(Vector2 currentPosition)
    {
        if (!useDetectionRange)
            return true;

        if (target == null)
            return false;

        if (detectionRange <= 0f)
            return true;

        return ((Vector2)target.position - currentPosition).sqrMagnitude <= detectionRange * detectionRange;
    }

    private float GetNaturalSpeedMultiplier()
    {
        if (!useSpeedNoise || speedNoiseAmount <= 0f)
            return 1f;

        float sample = Mathf.PerlinNoise(speedNoiseSeed, Time.time * speedNoiseFrequency);
        float centered = sample * 2f - 1f;
        return Mathf.Max(0.2f, 1f + centered * speedNoiseAmount);
    }

    private void ClearPath()
    {
        path.Clear();
        waypointIndex = 0;
        hasLastTargetPathPosition = false;
        consecutiveStuckRepaths = 0;
        hasLastStuckCheckPosition = false;
    }

    private void ClampBodyToRoom()
    {
        if (!clampToRoomBounds || body == null || room == null)
            return;

        Bounds bounds = GetPaddedRoomBounds();
        Vector2 clamped = ClampToBounds(body.position, bounds);
        if ((clamped - body.position).sqrMagnitude <= 0.0001f)
            return;

        body.position = clamped;
        body.linearVelocity = Vector2.zero;
    }

    private Bounds GetPaddedRoomBounds()
    {
        if (room == null)
            return new Bounds(transform.position, Vector3.zero);

        Bounds bounds = room.Bounds;
        float padding = Mathf.Max(roomEdgePadding, GetEffectiveAgentRadius());
        bounds.Expand(new Vector3(-padding * 2f, -padding * 2f, 0f));
        return bounds;
    }

    private static Vector2 ClampToBounds(Vector2 point, Bounds bounds)
    {
        return new Vector2(
            Mathf.Clamp(point.x, bounds.min.x, bounds.max.x),
            Mathf.Clamp(point.y, bounds.min.y, bounds.max.y));
    }

    private float Heuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        if (!allowDiagonalMovement)
            return dx + dy;

        int min = Mathf.Min(dx, dy);
        int max = Mathf.Max(dx, dy);
        return 1.4142135f * min + (max - min);
    }

    private float GetEffectiveWaypointReachDistance()
    {
        return Mathf.Max(waypointReachDistance, cellSize * 0.35f);
    }

    private float GetEffectiveAgentRadius()
    {
        float radius = agentRadius;

        if (!sizeAgentFromCollider)
            return radius;

        if (movementCollider == null || movementCollider.isTrigger)
            movementCollider = FindMovementCollider();

        if (movementCollider == null)
            return radius;

        Vector2 extents = movementCollider.bounds.extents;
        float colliderRadius = useColliderBoundingCircle
            ? extents.magnitude
            : Mathf.Max(extents.x, extents.y);

        return Mathf.Max(radius, colliderRadius + colliderClearancePadding);
    }

    private bool TryGetBoxCastData(Vector2 bodyPosition, out Vector2 center, out Vector2 size, out float angle)
    {
        center = bodyPosition;
        size = Vector2.zero;
        angle = 0f;

        if (!useColliderShapeForClearance || movementCollider is not BoxCollider2D boxCollider)
            return false;

        Vector2 currentBodyPosition = body != null ? body.position : (Vector2)transform.position;
        Vector2 centerOffset = (Vector2)boxCollider.bounds.center - currentBodyPosition;
        center = bodyPosition + centerOffset;

        Vector3 lossyScale = boxCollider.transform.lossyScale;
        size = new Vector2(
            Mathf.Abs(boxCollider.size.x * lossyScale.x),
            Mathf.Abs(boxCollider.size.y * lossyScale.y));

        float padding = colliderClearancePadding * 2f;
        size += new Vector2(padding, padding);
        angle = boxCollider.transform.eulerAngles.z;
        return true;
    }

    private static float DistancePointToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 segment = b - a;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared <= 0.0001f)
            return Vector2.Distance(point, a);

        float t = Mathf.Clamp01(Vector2.Dot(point - a, segment) / lengthSquared);
        Vector2 projection = a + segment * t;
        return Vector2.Distance(point, projection);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (drawRoomGizmos && room != null)
        {
            Bounds bounds = GetPaddedRoomBounds();
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.12f);
            Gizmos.DrawCube(bounds.center, bounds.size);
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        if (useDetectionRange && detectionRange > 0f)
        {
            Gizmos.color = new Color(0.95f, 0.9f, 0.25f, 0.16f);
            Gizmos.DrawSphere(transform.position, detectionRange);
            Gizmos.color = new Color(0.95f, 0.9f, 0.25f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }

        if (drawPathGizmos && path.Count > 0)
        {
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.95f);
            Vector3 previous = transform.position;
            for (int i = waypointIndex; i < path.Count; i++)
            {
                Vector3 current = path[i];
                Gizmos.DrawSphere(current, 0.08f);
                Gizmos.DrawLine(previous, current);
                previous = current;
            }
        }

        if (drawSampleGizmos && room != null)
        {
            GridInfo grid = new GridInfo(GetPaddedRoomBounds(), cellSize);
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    Vector2 world = grid.CellToWorld(cell);
                    Gizmos.color = IsBlocked(world)
                        ? new Color(1f, 0.1f, 0.1f, 0.25f)
                        : new Color(0.1f, 1f, 0.2f, 0.08f);
                    Gizmos.DrawCube(world, Vector3.one * cellSize * 0.8f);
                }
            }
        }
    }
#endif

    private readonly struct GridInfo
    {
        private readonly Vector2 origin;

        public GridInfo(Bounds bounds, float cellSize)
        {
            origin = bounds.min;
            CellSize = cellSize;
            Width = Mathf.Max(0, Mathf.FloorToInt(bounds.size.x / cellSize) + 1);
            Height = Mathf.Max(0, Mathf.FloorToInt(bounds.size.y / cellSize) + 1);
        }

        public int Width { get; }
        public int Height { get; }
        public int CellCount => Width * Height;
        private float CellSize { get; }

        public bool Contains(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height;
        }

        public int Index(Vector2Int cell)
        {
            return cell.y * Width + cell.x;
        }

        public Vector2Int WorldToCell(Vector2 world)
        {
            Vector2 local = world - origin;
            return new Vector2Int(
                Mathf.Clamp(Mathf.FloorToInt(local.x / CellSize), 0, Width - 1),
                Mathf.Clamp(Mathf.FloorToInt(local.y / CellSize), 0, Height - 1));
        }

        public Vector2 CellToWorld(Vector2Int cell)
        {
            return origin + new Vector2(
                (cell.x + 0.5f) * CellSize,
                (cell.y + 0.5f) * CellSize);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

// Enhanced MeshGenerator with collision, player control, enemies, powerups, and UI
public class EnhancedMeshGenerator : MonoBehaviour
{
    [Header("Mesh Settings")]
    public Material material;
    public int instanceCount = 100;
    private Mesh cubeMesh;
    private Mesh sphereMesh;
    private Mesh diamondMesh;
    private List<Matrix4x4> matrices = new List<Matrix4x4>();
    private List<int> colliderIds = new List<int>();
    
    public float width = 1f;
    public float height = 1f;
    public float depth = 1f;
    
    [Header("Player Settings")]
    public float movementSpeed = 5f;
    public float gravity = 9.8f;
    public int maxHealth = 100;
    public int maxLives = 3;
    
    private int playerID = -1;
    private Vector3 playerVelocity = Vector3.zero;
    private bool isGrounded = false;
    private int currentHealth;
    private int currentLives;
    private float invulnerabilityTime = 0f;
    public float invulnerabilityDuration = 1f;
    private bool isInvincible = false;
    private float invincibilityTime = 0f;
    private float playerFacingDirection = 1f; // 1 for right, -1 for left
    
    [Header("Camera")]
    public PlayerCameraFollow cameraFollow;
    
    [Header("World Settings")]
    public float constantZPosition = 0f;
    public float minX = -50f;
    public float maxX = 50f;
    public float minY = -50f;
    public float maxY = 50f;
    
    [Header("Ground")]
    public float groundY = -20f;
    public float groundWidth = 200f;
    public float groundDepth = 200f;

    [Header("Jump")]
    public float jumpForce = 18f;
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;
    
    [Header("Enemy Settings")]
    public int maxEnemies = 10;
    public float enemySpawnInterval = 5f;
    public float enemySpawnRadius = 20f;
    public float enemySpeed = 2f;
    public int enemyDamage = 10;
    
    private List<Enemy> enemies = new List<Enemy>();
    private float enemySpawnTimer = 0f;
    
    [Header("Powerup Settings")]
    public float powerupSpawnInterval = 8f;
    public int maxPowerups = 5;
    
    private List<Powerup> powerups = new List<Powerup>();
    private float powerupSpawnTimer = 0f;
    private List<Fireball> fireballs = new List<Fireball>();
    
    [Header("UI")]
    private GameUI gameUI;
    private float gameTime = 0f;

    void Start()
    {
        currentHealth = maxHealth;
        currentLives = maxLives;
        
        // Find or create camera if not assigned
        SetupCamera();

        // Create the cube mesh
        CreateCubeMesh();
        
        // Create sphere mesh for fireballs
        CreateSphereMesh();
        
        // Create diamond mesh for powerups
        CreateDiamondMesh();

        // Create player box
        CreatePlayer();

        // Create ground
        CreateGround();

        // Set up random boxes
        GenerateRandomBoxes();

        // Set up health bar and timer UI
        SetupUI();
    }
    
    void SetupUI()
    {
        GameObject uiObject = new GameObject("GameUI");
        gameUI = uiObject.AddComponent<GameUI>();
        gameUI.Initialize(maxHealth, maxLives);
    }
    
    void SetupCamera()
    {
        if (cameraFollow == null)
        {
            // Try to find existing camera
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                // Check if it already has our script
                cameraFollow = mainCamera.GetComponent<PlayerCameraFollow>();
                if (cameraFollow == null)
                {
                    // Add our script to existing camera
                    cameraFollow = mainCamera.gameObject.AddComponent<PlayerCameraFollow>();
                }
            }
            else
            {
                // No main camera found, create a new one
                GameObject cameraObj = new GameObject("PlayerCamera");
                Camera cam = cameraObj.AddComponent<Camera>();
                cameraFollow = cameraObj.AddComponent<PlayerCameraFollow>();

                // Set this as the main camera
                cam.tag = "MainCamera";
            }
            
            // Configure default camera settings
            cameraFollow.offset = new Vector3(0, 0, -15);
            cameraFollow.smoothSpeed = 0.1f;
        }
    }

    void CreateCubeMesh()
    {
        cubeMesh = new Mesh();
        
        // Create 8 vertices for the cube (corners)
        Vector3[] vertices = new Vector3[8]
        {
            // Bottom face vertices
            new Vector3(0, 0, 0),       // Bottom front left - 0
            new Vector3(width, 0, 0),   // Bottom front right - 1
            new Vector3(width, 0, depth),// Bottom back right - 2
            new Vector3(0, 0, depth),   // Bottom back left - 3
            
            // Top face vertices
            new Vector3(0, height, 0),       // Top front left - 4
            new Vector3(width, height, 0),   // Top front right - 5
            new Vector3(width, height, depth),// Top back right - 6
            new Vector3(0, height, depth)    // Top back left - 7
        };
        
        // Triangles for the 6 faces (2 triangles per face)
        int[] triangles = new int[36]
        {
            // Front face triangles (facing -Z)
            0, 4, 1,
            1, 4, 5,
            
            // Back face triangles (facing +Z)
            2, 6, 3,
            3, 6, 7,
            
            // Left face triangles (facing -X)
            0, 3, 4,
            4, 3, 7,
            
            // Right face triangles (facing +X)
            1, 5, 2,
            2, 5, 6,
            
            // Bottom face triangles (facing -Y)
            0, 1, 3,
            3, 1, 2,
            
            // Top face triangles (facing +Y)
            4, 7, 5,
            5, 7, 6
        };
        
        Vector2[] uvs = new Vector2[8];
        for (int i = 0; i < 8; i++)
        {
            uvs[i] = new Vector2(vertices[i].x / width, vertices[i].z / depth);
        }

        cubeMesh.vertices = vertices;
        cubeMesh.triangles = triangles;
        cubeMesh.uv = uvs;
        cubeMesh.RecalculateNormals();
        cubeMesh.RecalculateBounds();
    }
    
    void CreateSphereMesh()
    {
        // Create a simple sphere mesh using icosphere-like approach
        sphereMesh = new Mesh();
        
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        
        // Create sphere using UV sphere method (latitude/longitude)
        int latitudes = 8;
        int longitudes = 12;
        float radius = 0.5f;
        
        // Generate vertices
        for (int lat = 0; lat <= latitudes; lat++)
        {
            float theta = lat * Mathf.PI / latitudes;
            float sinTheta = Mathf.Sin(theta);
            float cosTheta = Mathf.Cos(theta);
            
            for (int lon = 0; lon <= longitudes; lon++)
            {
                float phi = lon * 2 * Mathf.PI / longitudes;
                float sinPhi = Mathf.Sin(phi);
                float cosPhi = Mathf.Cos(phi);
                
                float x = cosPhi * sinTheta;
                float y = cosTheta;
                float z = sinPhi * sinTheta;
                
                vertices.Add(new Vector3(x * radius, y * radius, z * radius));
            }
        }
        
        // Generate triangles
        for (int lat = 0; lat < latitudes; lat++)
        {
            for (int lon = 0; lon < longitudes; lon++)
            {
                int first = lat * (longitudes + 1) + lon;
                int second = first + longitudes + 1;
                
                triangles.Add(first);
                triangles.Add(second);
                triangles.Add(first + 1);
                
                triangles.Add(second);
                triangles.Add(second + 1);
                triangles.Add(first + 1);
            }
        }
        
        sphereMesh.vertices = vertices.ToArray();
        sphereMesh.triangles = triangles.ToArray();
        sphereMesh.RecalculateNormals();
        sphereMesh.RecalculateBounds();
    }
    
    void CreateDiamondMesh()
    {
        // Create a diamond/rhombus mesh (octahedron)
        diamondMesh = new Mesh();
        
        float size = 0.5f;
        
        // 6 vertices: top, bottom, and 4 middle points
        Vector3[] vertices = new Vector3[6]
        {
            new Vector3(0, size, 0),        // Top - 0
            new Vector3(0, -size, 0),       // Bottom - 1
            new Vector3(size, 0, 0),        // Right - 2
            new Vector3(0, 0, size),        // Back - 3
            new Vector3(-size, 0, 0),       // Left - 4
            new Vector3(0, 0, -size)        // Front - 5
        };
        
        // 8 triangular faces (4 on top pyramid, 4 on bottom pyramid)
        int[] triangles = new int[24]
        {
            // Top pyramid
            0, 2, 5,  // Top-Right-Front
            0, 5, 4,  // Top-Front-Left
            0, 4, 3,  // Top-Left-Back
            0, 3, 2,  // Top-Back-Right
            
            // Bottom pyramid
            1, 5, 2,  // Bottom-Front-Right
            1, 4, 5,  // Bottom-Left-Front
            1, 3, 4,  // Bottom-Back-Left
            1, 2, 3   // Bottom-Right-Back
        };
        
        diamondMesh.vertices = vertices;
        diamondMesh.triangles = triangles;
        diamondMesh.RecalculateNormals();
        diamondMesh.RecalculateBounds();
    }
    
    void CreatePlayer()
    {
        // Create player at a specific position
        Vector3 playerPosition = new Vector3(0, 10, constantZPosition);
        Vector3 playerScale = Vector3.one;
        Quaternion playerRotation = Quaternion.identity;
        
        // Register with collision system - properly handle width/height/depth
        playerID = CollisionManager.Instance.RegisterCollider(
            playerPosition, 
            new Vector3(width * playerScale.x, height * playerScale.y, depth * playerScale.z), 
            true);
        
        // Create transformation matrix
        Matrix4x4 playerMatrix = Matrix4x4.TRS(playerPosition, playerRotation, playerScale);
        matrices.Add(playerMatrix);
        colliderIds.Add(playerID);
        
        // Update the matrix in collision manager
        CollisionManager.Instance.UpdateMatrix(playerID, playerMatrix);
    }
    
    void CreateGround()
    {
        // Create a large ground plane
        Vector3 groundPosition = new Vector3(0, groundY, constantZPosition);
        Vector3 groundScale = new Vector3(groundWidth, 1f, groundDepth);
        Quaternion groundRotation = Quaternion.identity;
        
        // Register with collision system - use actual dimensions
        int groundID = CollisionManager.Instance.RegisterCollider(
            groundPosition, 
            new Vector3(groundWidth, 1f, groundDepth), 
            false);
        
        // Create transformation matrix
        Matrix4x4 groundMatrix = Matrix4x4.TRS(groundPosition, groundRotation, groundScale);
        matrices.Add(groundMatrix);
        colliderIds.Add(groundID);
        
        // Update the matrix in collision manager
        CollisionManager.Instance.UpdateMatrix(groundID, groundMatrix);
    }
    
    void GenerateRandomBoxes()
    {
        // Create random boxes (excluding player and ground)
        for (int i = 0; i < instanceCount - 2; i++)
        {
            // Random position (constant Z)
            Vector3 position = new Vector3(
                Random.Range(minX, maxX),
                Random.Range(minY, maxY),
                constantZPosition
            );
            
            // Random rotation only around Z axis
            Quaternion rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            
            // Random non-uniform scale - different for each dimension
            Vector3 scale = new Vector3(
                Random.Range(0.5f, 3f),
                Random.Range(0.5f, 3f),
                Random.Range(0.5f, 3f)
            );
            
            // Register with collision system - properly handle rectangular shapes
            int id = CollisionManager.Instance.RegisterCollider(
                position, 
                new Vector3(width * scale.x, height * scale.y, depth * scale.z), 
                false);
            
            // Create transformation matrix
            Matrix4x4 boxMatrix = Matrix4x4.TRS(position, rotation, scale);
            matrices.Add(boxMatrix);
            colliderIds.Add(id);
            
            // Update the matrix in collision manager
            CollisionManager.Instance.UpdateMatrix(id, boxMatrix);
        }
    }

    void Update()
    {
        if (currentHealth <= 0 && currentLives <= 0) return;
        
        gameTime += Time.deltaTime;
        gameUI.UpdateTimer(gameTime);
        
        if (invulnerabilityTime > 0)
        {
            invulnerabilityTime -= Time.deltaTime;
        }
        
        // Update invincibility
        if (invincibilityTime > 0)
        {
            invincibilityTime -= Time.deltaTime;
            if (invincibilityTime <= 0)
            {
                isInvincible = false;
                gameUI.SetInvincibility(false);
            }
        }
        
        // Handle fireball shooting
        if (Input.GetKeyDown(KeyCode.F))
        {
            ShootFireball();
        }
        
        UpdatePlayer();
        UpdateEnemies();
        UpdatePowerups();
        UpdateFireballs();
        SpawnEnemies();
        SpawnPowerups();
        RenderBoxes();
        RenderEnemies();
        RenderPowerups();
        RenderFireballs();
    }
    
    void ShootFireball()
    {
        Vector3 playerPos = GetPlayerPosition();
        // Shoot from the middle of the player in the direction they're facing
        Vector3 fireballPos = playerPos + new Vector3(playerFacingDirection * 0.1f, height * 0.5f, 0f);
        Fireball fireball = new Fireball(fireballPos, playerFacingDirection);
        fireballs.Add(fireball);
    }
    
    void UpdateFireballs()
    {
        for (int i = fireballs.Count - 1; i >= 0; i--)
        {
            Fireball fireball = fireballs[i];
            fireball.Update(Time.deltaTime);
            
            // Check collision with enemies
            bool hitEnemy = false;
            for (int j = enemies.Count - 1; j >= 0; j--)
            {
                if (Vector3.Distance(fireball.position, enemies[j].position) < 1.5f)
                {
                    enemies[j].Destroy();
                    enemies.RemoveAt(j);
                    hitEnemy = true;
                }
            }
            
            if (hitEnemy)
            {
                fireball.Destroy();
                fireballs.RemoveAt(i);
                continue;
            }
            
            // Remove fireballs that are too far away
            Vector3 playerPos = GetPlayerPosition();
            if (Mathf.Abs(fireball.position.x - playerPos.x) > 50f || fireball.position.y < groundY - 10f)
            {
                fireball.Destroy();
                fireballs.RemoveAt(i);
            }
        }
    }
    
    void RenderFireballs()
    {
        foreach (Fireball fireball in fireballs)
        {
            fireball.Render(sphereMesh);
        }
    }
    
    void SpawnPowerups()
    {
        if (powerups.Count >= maxPowerups) return;
        
        powerupSpawnTimer += Time.deltaTime;
        if (powerupSpawnTimer >= powerupSpawnInterval)
        {
            powerupSpawnTimer = 0f;
            SpawnPowerup();
        }
    }
    
    void SpawnPowerup()
    {
        Vector3 playerPos = GetPlayerPosition();
        
        // Spawn near player horizontally, but always above ground at reachable height
        float spawnX = playerPos.x + Random.Range(-10f, 15f);
        float spawnY = groundY + Random.Range(1.5f, 2f); // Lower spawn, easier to reach
        
        Vector3 spawnPos = new Vector3(spawnX, spawnY, constantZPosition);
        
        // Random powerup type - exclude Fireball (only ExtraLife and Invincibility)
        PowerupType type = (PowerupType)Random.Range(1, 3); // 1 = ExtraLife, 2 = Invincibility
        Powerup powerup = new Powerup(spawnPos, type);
        powerups.Add(powerup);
    }
    
    void UpdatePowerups()
    {
        Vector3 playerPos = GetPlayerPosition();
        
        for (int i = powerups.Count - 1; i >= 0; i--)
        {
            Powerup powerup = powerups[i];
            powerup.Update(Time.deltaTime);
            
            // Check collision with player
            if (Vector3.Distance(powerup.position, playerPos) < 1.5f)
            {
                ActivatePowerup(powerup.type);
                powerup.Destroy();
                powerups.RemoveAt(i);
            }
            
            // Remove powerups that fall too far
            if (powerup.position.y < groundY - 50f)
            {
                powerup.Destroy();
                powerups.RemoveAt(i);
            }
        }
    }
    
    void ActivatePowerup(PowerupType type)
    {
        switch (type)
        {
            case PowerupType.ExtraLife:
                currentLives++;
                gameUI.UpdateLives(currentLives);
                gameUI.ShowExtraLifeMessage();
                Debug.Log("Extra Life collected! Lives: " + currentLives);
                break;
                
            case PowerupType.Invincibility:
                isInvincible = true;
                invincibilityTime = 10f; // 10 seconds of invincibility
                gameUI.SetInvincibility(true);
                Debug.Log("Invincibility activated!");
                break;
                
            case PowerupType.Fireball:
                ShootFireball();
                Debug.Log("Fireball shot!");
                break;
        }
    }
    
    void RenderPowerups()
    {
        foreach (Powerup powerup in powerups)
        {
            powerup.Render(diamondMesh);
        }
    }
    
    void SpawnEnemies()
    {
        if (enemies.Count >= maxEnemies) return;
        
        enemySpawnTimer += Time.deltaTime;
        if (enemySpawnTimer >= enemySpawnInterval)
        {
            enemySpawnTimer = 0f;
            SpawnEnemy();
        }
    }
    
    void SpawnEnemy()
    {
        Vector3 playerPos = GetPlayerPosition();
        
        // Get camera bounds to spawn outside view
        Camera cam = Camera.main;
        if (cam == null) return;
        
        float cameraHeight = Mathf.Abs(cam.transform.position.z);
        float cameraHalfWidth = cameraHeight * cam.aspect;
        
        // Spawn ahead (to the right) of the camera view
        float spawnX = playerPos.x + cameraHalfWidth + Random.Range(2f, 10f);
        
        Vector3 spawnPos = new Vector3(
            spawnX,
            groundY + 2f,
            constantZPosition
        );
        
        Enemy enemy = new Enemy(spawnPos, enemySpeed, enemyDamage);
        enemies.Add(enemy);
    }
    
    void UpdateEnemies()
    {
        Vector3 playerPos = GetPlayerPosition();
        
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = enemies[i];
            enemy.Update(Time.deltaTime, playerPos);
            
            // Check collision with player
            if (Vector3.Distance(enemy.position, playerPos) < 1.5f)
            {
                if (isInvincible)
                {
                    // Kill enemy on contact
                    enemy.Destroy();
                    enemies.RemoveAt(i);
                }
                else if (invulnerabilityTime <= 0)
                {
                    TakeDamage(enemy.damage);
                    invulnerabilityTime = invulnerabilityDuration;
                }
            }
            
            // Remove enemies that fall too far
            if (enemy.position.y < groundY - 50f)
            {
                enemy.Destroy();
                enemies.RemoveAt(i);
            }
        }
    }
    
    void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;
        
        gameUI.UpdateHealth(currentHealth);
        
        if (currentHealth <= 0)
        {
            if (currentLives > 0)
            {
                // Respawn with full health
                currentLives--;
                currentHealth = maxHealth;
                gameUI.UpdateHealth(currentHealth);
                gameUI.UpdateLives(currentLives);
                
                // Reset player position
                RespawnPlayer();
            }
            else
            {
                OnPlayerDeath();
            }
        }
    }
    
    void RespawnPlayer()
    {
        Vector3 respawnPos = new Vector3(0, 10, constantZPosition);
        Matrix4x4 newMatrix = Matrix4x4.TRS(respawnPos, Quaternion.identity, Vector3.one);
        matrices[colliderIds.IndexOf(playerID)] = newMatrix;
        CollisionManager.Instance.UpdateCollider(playerID, respawnPos, new Vector3(width, height, depth));
        CollisionManager.Instance.UpdateMatrix(playerID, newMatrix);
        playerVelocity = Vector3.zero;
        invulnerabilityTime = invulnerabilityDuration * 2f; // Extra invulnerability on respawn
    }
    
    void OnPlayerDeath()
    {
        Debug.Log("Game Over! Time survived: " + gameTime.ToString("F1") + " seconds");
        // Could add game over UI here
    }
    
    void RenderEnemies()
    {
        foreach (Enemy enemy in enemies)
        {
            enemy.Render(cubeMesh);
        }
    }
    
    Vector3 GetPlayerPosition()
    {
        if (playerID == -1) return Vector3.zero;
        
        Matrix4x4 playerMatrix = matrices[colliderIds.IndexOf(playerID)];
        return playerMatrix.GetPosition();
    }
    
    void UpdatePlayer()
    {
        if (playerID == -1) return;

        // Get current player matrix
        Matrix4x4 playerMatrix = matrices[colliderIds.IndexOf(playerID)];
        DecomposeMatrix(playerMatrix, out Vector3 pos, out Quaternion rot, out Vector3 scale);

        float horizontal = 0;
        if (Input.GetKey(KeyCode.A)) horizontal -= 1;
        if (Input.GetKey(KeyCode.D)) horizontal += 1;
        
        // Update facing direction based on movement input
        if (horizontal != 0)
        {
            playerFacingDirection = horizontal > 0 ? 1f : -1f;
        }

        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            playerVelocity.y = jumpForce;
            isGrounded = false;
        }

        if (playerVelocity.y < 0)
        {
            playerVelocity.y += (-gravity * 0.5f) * fallMultiplier * Time.deltaTime;
        }
        else if (playerVelocity.y > 0 && !Input.GetKey(KeyCode.Space))
        {
            playerVelocity.y -= gravity * lowJumpMultiplier * Time.deltaTime;
        }
        else
        {
            playerVelocity.y -= gravity * Time.deltaTime;
        }

        float horizontalSpeed = isGrounded ? movementSpeed : movementSpeed * 0.5f;

        Vector3 newPos = pos;
        newPos.x += horizontal * horizontalSpeed * Time.deltaTime;

        if (!CheckCollisionAt(playerID, new Vector3(newPos.x, pos.y, pos.z)))
        {
            pos.x = newPos.x;
        }

        newPos = pos;
        newPos.y += playerVelocity.y * Time.deltaTime;

        if (CheckCollisionAt(playerID, new Vector3(pos.x, newPos.y, pos.z)))
        {
            if (playerVelocity.y < 0)
            {
                isGrounded = true;
            }
            playerVelocity.y = 0;
        }
        else
        {
            pos.y = newPos.y;
            isGrounded = false;
        }

        Matrix4x4 newMatrix = Matrix4x4.TRS(pos, rot, scale);
        matrices[colliderIds.IndexOf(playerID)] = newMatrix;

        CollisionManager.Instance.UpdateCollider(playerID, pos,
            new Vector3(width * scale.x, height * scale.y, depth * scale.z));

        CollisionManager.Instance.UpdateMatrix(playerID, newMatrix);

        if (cameraFollow != null)
        {
            cameraFollow.SetPlayerPosition(pos);
        }
    }
    
    bool CheckCollisionAt(int id, Vector3 position)
    {
        return CollisionManager.Instance.CheckCollision(id, position, out _);
    }
    
    void RenderBoxes()
    {
        Matrix4x4[] matrixArray = matrices.ToArray();
        
        for (int i = 0; i < matrixArray.Length; i += 1023) {
            int batchSize = Mathf.Min(1023, matrixArray.Length - i);
            Matrix4x4[] batchMatrices = new Matrix4x4[batchSize];
            System.Array.Copy(matrixArray, i, batchMatrices, 0, batchSize);
            Graphics.DrawMeshInstanced(cubeMesh, 0, material, batchMatrices, batchSize);
        }
    }

    void DecomposeMatrix(Matrix4x4 matrix, out Vector3 position, out Quaternion rotation, out Vector3 scale)
    {
        position = matrix.GetPosition();
        rotation = matrix.rotation;
        scale = matrix.lossyScale;
    }
    
    public void AddRandomBox()
    {
        Vector3 position = new Vector3(
            Random.Range(minX, maxX),
            Random.Range(minY, maxY),
            constantZPosition
        );
        
        Quaternion rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        
        Vector3 scale = new Vector3(
            Random.Range(0.5f, 3f),
            Random.Range(0.5f, 3f),
            Random.Range(0.5f, 3f)
        );
        
        int id = CollisionManager.Instance.RegisterCollider(
            position, 
            new Vector3(width * scale.x, height * scale.y, depth * scale.z), 
            false);
        
        Matrix4x4 boxMatrix = Matrix4x4.TRS(position, rotation, scale);
        matrices.Add(boxMatrix);
        colliderIds.Add(id);
        
        CollisionManager.Instance.UpdateMatrix(id, boxMatrix);
    }
}

// Powerup types enum
public enum PowerupType
{
    Fireball,
    ExtraLife,
    Invincibility
}

// Powerup class
public class Powerup
{
    public Vector3 position;
    public PowerupType type;
    private Material powerupMaterial;
    private int colliderID;
    private Vector3 size = new Vector3(0.6f, 0.6f, 0.6f);
    private float rotationSpeed = 90f;
    private float rotation = 0f;
    private float bobSpeed = 2f;
    private float bobAmount = 0.3f;
    private float bobTimer = 0f;
    private Vector3 startPosition;
    
    public Powerup(Vector3 startPos, PowerupType powerupType)
    {
        position = startPos;
        startPosition = startPos;
        type = powerupType;
        
        // Register collider for powerup
        colliderID = CollisionManager.Instance.RegisterCollider(position, size, false);
        
        powerupMaterial = new Material(Shader.Find("Unlit/Color"));
        
        // Different colors for different powerup types
        switch (type)
        {
            case PowerupType.Fireball:
                powerupMaterial.color = new Color(1f, 0.5f, 0f); // Orange
                break;
            case PowerupType.ExtraLife:
                powerupMaterial.color = new Color(0f, 1f, 0f); // Green
                break;
            case PowerupType.Invincibility:
                powerupMaterial.color = new Color(0.5f, 0f, 1f); // Purple
                break;
        }
    }
    
    public void Update(float deltaTime)
    {
        // Rotate powerup
        rotation += rotationSpeed * deltaTime;
        
        // Bob up and down
        bobTimer += deltaTime;
        float bobOffset = Mathf.Sin(bobTimer * bobSpeed) * bobAmount;
        position.y = startPosition.y + bobOffset;
        
        CollisionManager.Instance.UpdateCollider(colliderID, position, size);
    }
    
    public void Destroy()
    {
        CollisionManager.Instance.RemoveCollider(colliderID);
    }
    
    public void Render(Mesh mesh)
    {
        Quaternion rot = Quaternion.Euler(0, 0, rotation);
        Matrix4x4 matrix = Matrix4x4.TRS(position, rot, Vector3.one * 0.6f);
        Graphics.DrawMesh(mesh, matrix, powerupMaterial, 0);
    }
}

// Fireball class
public class Fireball
{
    public Vector3 position;
    private float speed = 15f;
    private float direction; // 1 for right, -1 for left
    private Material fireballMaterial;
    private int colliderID;
    private Vector3 size = new Vector3(0.5f, 0.5f, 0.5f);
    
    public Fireball(Vector3 startPos, float dir)
    {
        position = startPos;
        direction = dir;
        
        // Register collider for fireball
        colliderID = CollisionManager.Instance.RegisterCollider(position, size, false);
        
        fireballMaterial = new Material(Shader.Find("Unlit/Color"));
        fireballMaterial.color = new Color(1f, 0.3f, 0f); // Bright orange
    }
    
    public void Update(float deltaTime)
    {
        // Move fireball in straight line
        position.x += direction * speed * deltaTime;
        
        CollisionManager.Instance.UpdateCollider(colliderID, position, size);
    }
    
    public void Destroy()
    {
        CollisionManager.Instance.RemoveCollider(colliderID);
    }
    
    public void Render(Mesh mesh)
    {
        Matrix4x4 matrix = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * 0.5f);
        Graphics.DrawMesh(mesh, matrix, fireballMaterial, 0);
    }
}

// Enemy class
public class Enemy
{
    public Vector3 position;
    public float speed;
    public int damage;
    public float direction;
    private Material enemyMaterial;
    private float changeDirectionTimer;
    private float changeDirectionInterval = 2f;
    private int colliderID;
    private Vector3 velocity;
    private float gravity = 9.8f;
    private Vector3 size = new Vector3(0.8f, 0.8f, 0.8f);
    
    public Enemy(Vector3 startPos, float moveSpeed, int dmg)
    {
        position = startPos;
        speed = moveSpeed;
        damage = dmg;
        direction = Random.value > 0.5f ? 1f : -1f;
        changeDirectionTimer = Random.Range(0f, changeDirectionInterval);
        velocity = Vector3.zero;
        
        // Register collider for enemy
        colliderID = CollisionManager.Instance.RegisterCollider(position, size, false);
        
        enemyMaterial = new Material(Shader.Find("Unlit/Color"));
        enemyMaterial.color = new Color(1f, 0f, 0f);
    }
    
    public void Update(float deltaTime, Vector3 playerPos)
    {
        changeDirectionTimer += deltaTime;
        
        if (changeDirectionTimer >= changeDirectionInterval)
        {
            changeDirectionTimer = 0f;
            
            // 70% chance to move towards player, 30% random
            if (Random.value > 0.3f)
            {
                direction = playerPos.x > position.x ? 1f : -1f;
            }
            else
            {
                direction *= -1f;
            }
        }
        
        // Horizontal movement with collision check
        Vector3 newPos = position;
        newPos.x += direction * speed * deltaTime;
        
        if (!CollisionManager.Instance.CheckCollision(colliderID, new Vector3(newPos.x, position.y, position.z), out _))
        {
            position.x = newPos.x;
        }
        else
        {
            // Hit a wall, reverse direction
            direction *= -1f;
        }
        
        // Apply gravity
        velocity.y -= gravity * deltaTime;
        
        // Vertical movement with collision check
        newPos = position;
        newPos.y += velocity.y * deltaTime;
        
        if (CollisionManager.Instance.CheckCollision(colliderID, new Vector3(position.x, newPos.y, position.z), out _))
        {
            velocity.y = 0;
        }
        else
        {
            position.y = newPos.y;
        }
        
        CollisionManager.Instance.UpdateCollider(colliderID, position, size);
    }
    
    public void Destroy()
    {
        CollisionManager.Instance.RemoveCollider(colliderID);
    }
    
    public void Render(Mesh mesh)
    {
        Matrix4x4 matrix = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * 0.8f);
        Graphics.DrawMesh(mesh, matrix, enemyMaterial, 0);
    }
}

// UI Manager
public class GameUI : MonoBehaviour
{
    private Rect healthBarRect = new Rect(20, 20, 200, 30);
    private Rect timerRect;
    private Rect livesRect;
    private int currentHealth;
    private int maxHealth;
    private float gameTime;
    private int currentLives;
    private bool isInvincible = false;
    private GUIStyle timerStyle;
    private GUIStyle livesStyle;
    private GUIStyle invincibleStyle;
    private GUIStyle extraLifeStyle;
    private GUIStyle bgStyle;
    private float extraLifeMessageTime = 0f;
    private float extraLifeMessageDuration = 2f;
    
    public void Initialize(int maxHp, int maxLives)
    {
        maxHealth = maxHp;
        currentHealth = maxHp;
        currentLives = maxLives;
        
        timerStyle = new GUIStyle();
        timerStyle.fontSize = 20;
        timerStyle.normal.textColor = Color.white;
        timerStyle.fontStyle = FontStyle.Bold;
        timerStyle.alignment = TextAnchor.MiddleCenter;
        
        livesStyle = new GUIStyle();
        livesStyle.fontSize = 18;
        livesStyle.normal.textColor = Color.white;
        livesStyle.fontStyle = FontStyle.Bold;
        livesStyle.alignment = TextAnchor.MiddleLeft;
        
        invincibleStyle = new GUIStyle();
        invincibleStyle.fontSize = 24;
        invincibleStyle.normal.textColor = new Color(1f, 0.5f, 1f);
        invincibleStyle.fontStyle = FontStyle.Bold;
        invincibleStyle.alignment = TextAnchor.MiddleCenter;
        
        extraLifeStyle = new GUIStyle();
        extraLifeStyle.fontSize = 28;
        extraLifeStyle.normal.textColor = new Color(0f, 1f, 0f);
        extraLifeStyle.fontStyle = FontStyle.Bold;
        extraLifeStyle.alignment = TextAnchor.MiddleCenter;
        
        bgStyle = new GUIStyle();
        bgStyle.normal.background = MakeTex(2, 2, new Color(0, 0, 0, 0.5f));
    }
    
    public void UpdateHealth(int health)
    {
        currentHealth = health;
    }
    
    public void UpdateLives(int lives)
    {
        currentLives = lives;
    }
    
    public void UpdateTimer(float time)
    {
        gameTime = time;
    }
    
    public void SetInvincibility(bool invincible)
    {
        isInvincible = invincible;
    }
    
    public void ShowExtraLifeMessage()
    {
        extraLifeMessageTime = extraLifeMessageDuration;
    }
    
    void OnGUI()
    {
        // Update extra life message timer
        if (extraLifeMessageTime > 0)
        {
            extraLifeMessageTime -= Time.deltaTime;
        }
        
        // Health bar
        Texture2D blackTex = MakeTex(2, 2, Color.black);
        GUI.DrawTexture(new Rect(healthBarRect.x - 2, healthBarRect.y - 2, healthBarRect.width + 4, healthBarRect.height + 4), blackTex);
        
        float healthPercent = (float)currentHealth / maxHealth;
        Color healthColor = Color.Lerp(Color.red, Color.green, healthPercent);
        Texture2D healthTex = MakeTex(2, 2, healthColor);
        GUI.DrawTexture(new Rect(healthBarRect.x, healthBarRect.y, healthBarRect.width * healthPercent, healthBarRect.height), healthTex);
        
        // Lives counter
        livesRect = new Rect(20, 60, 150, 30);
        GUI.Label(livesRect, "Lives: " + currentLives, livesStyle);
        
        // Timer
        int minutes = Mathf.FloorToInt(gameTime / 60f);
        int seconds = Mathf.FloorToInt(gameTime % 60f);
        string timerText = $"{minutes:00}:{seconds:00}";
        
        float timerWidth = 120f;
        timerRect = new Rect(Screen.width - timerWidth - 20, 20, timerWidth, 30);
        
        GUI.Box(new Rect(timerRect.x - 5, timerRect.y - 5, timerRect.width + 10, timerRect.height + 10), "", bgStyle);
        GUI.Label(timerRect, timerText, timerStyle);
        
        // Invincibility indicator
        if (isInvincible)
        {
            Rect invincibleRect = new Rect(Screen.width / 2 - 100, 20, 200, 40);
            GUI.Box(new Rect(invincibleRect.x - 5, invincibleRect.y - 5, invincibleRect.width + 10, invincibleRect.height + 10), "", bgStyle);
            GUI.Label(invincibleRect, "INVINCIBLE!", invincibleStyle);
        }
        
        // Extra life message
        if (extraLifeMessageTime > 0)
        {
            Rect extraLifeRect = new Rect(Screen.width / 2 - 150, Screen.height / 2 - 50, 300, 50);
            GUI.Box(new Rect(extraLifeRect.x - 5, extraLifeRect.y - 5, extraLifeRect.width + 10, extraLifeRect.height + 10), "", bgStyle);
            GUI.Label(extraLifeRect, "+ EXTRA LIFE!", extraLifeStyle);
        }
        
        // Controls help
        GUIStyle helpStyle = new GUIStyle();
        helpStyle.fontSize = 14;
        helpStyle.normal.textColor = Color.white;
        helpStyle.alignment = TextAnchor.LowerLeft;
        
        string helpText = "Controls: A/D - Move, Space - Jump, F - Fireball";
        Rect helpRect = new Rect(20, Screen.height - 40, 400, 30);
        GUI.Label(helpRect, helpText, helpStyle);
    }
    
    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
        {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}
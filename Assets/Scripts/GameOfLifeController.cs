using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOfLifeController : MonoBehaviour {
    [System.Serializable]
    public class Pattern {
        public string name;
        public Vector2Int[] cells;
    }

    [Header("Настройки сетки")]
    public int gridWidth = 20;
    public int gridHeight = 20;
    public float cellSize = 1f;
    public GameObject cellPrefab;

    [Header("Simulation settings")]
    public float simulationDelay = 1.0f;

    [Header("Speed settings")]
    public TextMeshProUGUI speedDisplayText;
    private float[] speedLevels = { 2.0f, 1.0f, 0.67f, 0.5f, 0.4f, 0.33f };
    private string[] speedLabels = { "0.5x", "1.0x", "1.5x", "2.0x", "2.5x", "3.0x" };
    private int currentSpeedIndex = 1;
    
    [Header("Predefined shapes")]
    public Pattern[] availablePatterns;
    private Pattern currentPattern = null;
    private bool isPlacingPattern = false;

    [Header("Game state")]
    private Cell[,] grid;
    private bool isSimulating = false;
    private Coroutine simulationCoroutine;
    private bool isGridInitialized = false;

    private CameraController3D cameraController;

    public void SelectGlider() {
        if (availablePatterns.Length > 0) {
            currentPattern = availablePatterns[0];
            isPlacingPattern = true;
        }
    }

    public void SelectSpaceShip() {
        if (availablePatterns.Length > 1) {
            currentPattern = availablePatterns[1];
            isPlacingPattern = true;
        }
    }

    public void SelectSingleCell() {
        currentPattern = null;
        isPlacingPattern = false;
    }

    void Awake() {
        InitializeGrid();
    }

    void Start() {
        simulationDelay = speedLevels[currentSpeedIndex];
        SetupCamera();
        UpdateSpeedDisplay();
    }

    private void InitializeGrid() {
        if (!isGridInitialized) {
            CreateGrid();
            isGridInitialized = true;
        }
    }

    void SetupCamera() {
        cameraController = Camera.main.GetComponent<CameraController3D>();
        Vector3 gridCenter = new Vector3(gridWidth * cellSize / 2, 0, gridHeight * cellSize / 2);
        cameraController.SetGridParameters(gridCenter, gridWidth * cellSize, gridHeight * cellSize);
    }

    public void IncreaseSpeed() {
        if (currentSpeedIndex < speedLevels.Length - 1) {
            ++currentSpeedIndex;
            simulationDelay = speedLevels[currentSpeedIndex];
            UpdateSpeedDisplay();
            RestartSimulationIfRunning();
        }
    }

    public void DecreaseSpeed() {
        if (currentSpeedIndex > 0) {
            --currentSpeedIndex;
            simulationDelay = speedLevels[currentSpeedIndex];
            UpdateSpeedDisplay();
            RestartSimulationIfRunning();
        }
    }

    void UpdateSpeedDisplay() {
        speedDisplayText.text = $"Скорость: {speedLabels[currentSpeedIndex]}";
    }

    void RestartSimulationIfRunning() {
        if (isSimulating) {
            PauseSimulation();
            StartSimulation();
        }
    }

    void CreateGrid() {
        grid = new Cell[gridWidth, gridHeight];

        for (int y = 0; y < gridHeight; ++y) {
            for (int x = 0; x < gridWidth; ++x) {
                Vector3 position = new Vector3(x * cellSize, 0f, y * cellSize);
                GameObject cellObj = Instantiate(cellPrefab, position, Quaternion.identity);
                cellObj.transform.parent = transform;
                grid[x, y] = cellObj.GetComponent<Cell>();
            }
        }
    }

    int CountAliveNeighbors(int x, int y) {
        int count = 0;

        for (int i = -1; i <= 1; ++i) {
            for (int j = -1; j <= 1; ++j) {
                if (i == 0 && j == 0) continue;

                int neighborX = x + i;
                int neighborY = y + j;

                if (neighborX >= 0 && neighborX < gridWidth &&
                    neighborY >= 0 && neighborY < gridHeight) {
                    if (grid[neighborX, neighborY].isAlive) {
                        ++count;
                    }
                }
            }
        }

        return count;
    }

    void CalculateNextGeneration() {
        bool[,] nextState = new bool[gridWidth, gridHeight];

        for (int y = 0; y < gridHeight; ++y) {
            for (int x = 0; x < gridWidth; ++x) {
                int aliveNeighbors = CountAliveNeighbors(x, y);
                bool currentlyAlive = grid[x, y].isAlive;

                if (currentlyAlive && (aliveNeighbors == 2 || aliveNeighbors == 3)) {
                    nextState[x, y] = true;
                } else if (!currentlyAlive && aliveNeighbors == 3) {
                    nextState[x, y] = true;
                } else {
                    nextState[x, y] = false;
                }
            }
        }

        for (int y = 0; y < gridHeight; ++y) {
            for (int x = 0; x < gridWidth; ++x) {
                grid[x, y].SetAlive(nextState[x, y]);
            }
        }
    }

    IEnumerator SimulationRoutine() {
        while (isSimulating) {
            CalculateNextGeneration();
            yield return new WaitForSeconds(simulationDelay);
        }
    }

    public void StartSimulation() {
        if (!isSimulating) {
            isSimulating = true;
            simulationCoroutine = StartCoroutine(SimulationRoutine());
        }
    }

    public void PauseSimulation() {
        if (isSimulating) {
            isSimulating = false;
            if (simulationCoroutine != null) {
                StopCoroutine(simulationCoroutine);
            }
        }
    }

    public void RandomizeGrid() {
        PauseSimulation();
        InitializeGrid();
        for (int y = 0; y < gridHeight; ++y) {
            for (int x = 0; x < gridWidth; ++x) {
                grid[x, y].SetAlive(Random.Range(0, 2) == 0);
            }
        }
    }

    public void ClearGrid() {
        PauseSimulation();
        InitializeGrid();
        for (int y = 0; y < gridHeight; ++y) {
            for (int x = 0; x < gridWidth; ++x) {
                grid[x, y].SetAlive(false);
            }
        }
    }
    
    void Update() {
        if (!isSimulating && Input.GetMouseButtonDown(0)) {
            HandleMouseClick();
        }
    }

    void HandleMouseClick() {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit)) {
            Cell cell = hit.collider.GetComponent<Cell>();
            Vector3 worldPos = cell.transform.position;
            int cellX = Mathf.RoundToInt(worldPos.x / cellSize);
            int cellY = Mathf.RoundToInt(worldPos.z / cellSize);
            
            if (isPlacingPattern && currentPattern != null) {
                TryPlacePattern(currentPattern, cellX, cellY);
            } else {
                cell.SetAlive(!cell.isAlive);
            }
        }
    }

    private void TryPlacePattern(Pattern pattern, int centerX, int centerY) {
        if (pattern == null) return;

        foreach (Vector2Int cell in pattern.cells) {
            int targetX = centerX + cell.x;
            int targetY = centerY + cell.y;

            if (targetX < 0 || targetX >= gridWidth || targetY < 0 || targetY >= gridHeight) {
                Debug.Log("Фигура выходит за границы сетки");
                return;
            }

            if (grid[targetX, targetY].isAlive) {
                Debug.Log("Некоторые клетки фигуры заняты");
                return;
            }
        }

        int cellsPlaced = 0;
        foreach (Vector2Int cell in pattern.cells) {
            int targetX = centerX + cell.x;
            int targetY = centerY + cell.y;

            grid[targetX, targetY].SetAlive(true, Cell.CellColor.Dead);
            ++cellsPlaced;
        }
    }
    
    public void StopAllCellAnimations() {
        if (grid == null) return;
        foreach (Cell cell in grid) {
            if (cell != null) {
                cell.StopAnimations();
            }
        }
    }

    public void ForceResetAllCells() {
        if (grid == null) return;
        foreach (Cell cell in grid) {
            if (cell != null) {
                cell.ForceResetState();
            }
        }
    }
}
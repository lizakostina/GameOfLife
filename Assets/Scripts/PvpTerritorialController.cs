using System.Collections;
using UnityEngine;
using TMPro;

public class PvPTerritorialController : MonoBehaviour {
    [System.Serializable]
    public class Pattern {
        public string name;
        public Vector2Int[] cells;
    }

    [Header("Greed settings")]
    public int gridWidth = 20;
    public int gridHeight = 20;
    public float cellSize = 1f;
    public GameObject cellPrefab;

    [Header("Simulation settings")]
    public float simulationDelay = 1.0f;

    [Header("Pvp - territorial control")]
    public int initialCellsPerPlayer = 20;
    public int totalGenerations = 25;
    private int blueScore = 0;
    private int redScore = 0;
    private int currentGeneration = 0;
    private bool isBlueTurn = true;
    private int blueCellsPlaced = 0;
    private int redCellsPlaced = 0;
    private GamePhase gamePhase = GamePhase.Setup;
    
    [Header("Predefined shapes")]
    public Pattern[] availablePatterns;
    private Pattern currentPattern = null;
    private bool isPlacingPattern = false;
    
    [Header("UI Elements")]
    public TextMeshProUGUI speedDisplayText;
    public TextMeshProUGUI blueScoreText;
    public TextMeshProUGUI redScoreText;
    public TextMeshProUGUI gameStateText;
    
    [Header("Result Panel")]
    public GameObject resultPanel;
    public TextMeshProUGUI winnerText;
    public TextMeshProUGUI resultScoreText;

    [Header("Rules Panel")]
    public GameObject rulesPanel;

    [Header("Speed settings")]
    private float[] speedLevels = { 2.0f, 1.0f, 0.6667f, 0.5f, 0.4f, 0.3333f };
    private string[] speedLabels = { "0.5x", "1.0x", "1.5x", "2.0x", "2.5x", "3.0x" };
    private int currentSpeedIndex = 1;

    [Header("Game state")]
    private Cell[,] grid;
    private bool isSimulating = false;
    private Coroutine simulationCoroutine;
    private bool isGridInitialized = false;
    private bool isDestroying = false;
    private enum GamePhase { Setup, Simulation, GameOver }

    private CameraController3D cameraController;

    // Методы для выбора фигур
    public void SelectGlider()
    {
        if (availablePatterns.Length > 0)
        {
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

    // Методы для окна результатов матча
    private void ShowResultPanel() {   
        string winner;
        if (blueScore > redScore) {
            winner = "СИНИЕ";
        } else if (redScore > blueScore) {
            winner = "КРАСНЫЕ";
        } else {
            winner = "НИЧЬЯ";
        }
        
        winnerText.text = $"ПОБЕДИТЕЛЬ: {winner}";
        resultScoreText.text = $"СИНИЕ: {blueScore} | КРАСНЫЕ: {redScore}";
        
        resultPanel.SetActive(true);
    }

    // Метод для подсчета клеток по цвету
    private int CountCellsByColor(Cell.CellColor color) {
        int count = 0;
        for (int y = 0; y < gridHeight; ++y) {
            for (int x = 0; x < gridWidth; ++x) {
                if (grid[x, y].isAlive && grid[x, y].color == color) {
                    ++count;
                }
            }
        }
        return count;
    }

    // Метод для подсчета очков
    private void CalculateScores() {
        blueScore = CountCellsByColor(Cell.CellColor.Blue);
        redScore = CountCellsByColor(Cell.CellColor.Red);
    }

    // Метод для скрытия окна результатов
    public void HideResultPanel() {
        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }
    }

    // Основные методы
    void Awake() {
        InitializeGrid();
    }

    void Start() {
        simulationDelay = speedLevels[currentSpeedIndex];
        SetupCamera();
        UpdateGameText();
        UpdateSpeedDisplay();
        HideResultPanel();
        if (rulesPanel != null)
            rulesPanel.SetActive(false);
        
    }

    void OnDisable() {
        StopAllCellAnimations();
        PauseSimulation();
    }

    void OnDestroy() {
        isDestroying = true;
        StopAllCellAnimations();
        PauseSimulation();
    }

    private void InitializeGrid() {
        if (!isGridInitialized) {
            CreateGrid();
            isGridInitialized = true;
        }
    }

    void SetupCamera() {
        cameraController = Camera.main?.GetComponent<CameraController3D>();
        if (cameraController != null) {
            Vector3 gridCenter = new Vector3(gridWidth * cellSize / 2, 0, gridHeight * cellSize / 2);
            cameraController.SetGridParameters(gridCenter, gridWidth * cellSize, gridHeight * cellSize);
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
                
                grid[x, y].SetAlive(false, Cell.CellColor.Dead);
            }
        }
    }

    // Управление скоростью
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
        if (speedDisplayText != null) {
            speedDisplayText.text = $"Скорость: {speedLabels[currentSpeedIndex]}";
        }
    }

    void RestartSimulationIfRunning() {
        if (isSimulating && gamePhase == GamePhase.Simulation) {
            PauseSimulation();
            StartSimulation();
        }
    }

    // PvP логика
    void Update() {
        if (gamePhase == GamePhase.Setup && Input.GetMouseButtonDown(0) && !isDestroying) {
            HandleSetupClick();
        }
    }

    void HandleSetupClick() {
        if (blueCellsPlaced >= initialCellsPerPlayer && redCellsPlaced >= initialCellsPerPlayer) {
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit)) {
            Cell cell = hit.collider.GetComponent<Cell>();
            if (cell != null && !cell.isAlive) {
                Vector3 worldPos = cell.transform.position;
                int cellX = Mathf.RoundToInt(worldPos.x / cellSize);
                int cellY = Mathf.RoundToInt(worldPos.z / cellSize);
                
                if (isPlacingPattern && currentPattern != null) {
                    PlacePattern(currentPattern, cellX, cellY);
                } else {
                    PlaceSingleCell(cellX, cellY);
                }
                
                CalculateScores();
                UpdateGameText();
            }
        }
    }

    // Метод для размещения фигуры
    private void PlacePattern(Pattern pattern, int centerX, int centerY) {
        if (pattern == null || isDestroying) return;
        
        int availableCells = isBlueTurn ? (initialCellsPerPlayer - blueCellsPlaced) : (initialCellsPerPlayer - redCellsPlaced);
        
        if (pattern.cells.Length > availableCells) {
            return;
        }
        
        foreach (Vector2Int cell in pattern.cells) {
            int targetX = centerX + cell.x;
            int targetY = centerY + cell.y;
            
            if (targetX < 0 || targetX >= gridWidth || targetY < 0 || targetY >= gridHeight) {
                return;
            }
            
            if (grid[targetX, targetY].isAlive) {
                return;
            }
        }
        
        int cellsPlaced = 0;
        foreach (Vector2Int cell in pattern.cells) {
            int targetX = centerX + cell.x;
            int targetY = centerY + cell.y;
            
            Cell.CellColor color = isBlueTurn ? Cell.CellColor.Blue : Cell.CellColor.Red;
            grid[targetX, targetY].SetAlive(true, color);
            ++cellsPlaced;
        }
        
        if (isBlueTurn) {
            blueCellsPlaced += cellsPlaced;
            if (blueCellsPlaced >= initialCellsPerPlayer) {
                isBlueTurn = false;
            }
        } else {
            redCellsPlaced += cellsPlaced;
        }
    }

    private void PlaceSingleCell(int x, int y) {
        if (isBlueTurn && blueCellsPlaced < initialCellsPerPlayer) {
            grid[x, y].SetAlive(true, Cell.CellColor.Blue);
            ++blueCellsPlaced;
            
            if (blueCellsPlaced >= initialCellsPerPlayer) {
                isBlueTurn = false;
            }
        } else if (!isBlueTurn && redCellsPlaced < initialCellsPerPlayer) {
            grid[x, y].SetAlive(true, Cell.CellColor.Red);
            ++redCellsPlaced;
        }
    }

    // Запуск симуляции
    public void StartSimulation() {   
        if (gamePhase == GamePhase.Setup && blueCellsPlaced >= initialCellsPerPlayer && redCellsPlaced >= initialCellsPerPlayer) {
            gamePhase = GamePhase.Simulation;
            isSimulating = true;
            simulationCoroutine = StartCoroutine(SimulationRoutine());
            UpdateGameText();
        } else if (gamePhase == GamePhase.Simulation && !isSimulating) {
            isSimulating = true;
            simulationCoroutine = StartCoroutine(SimulationRoutine());
            UpdateGameText();
        }
    }

    public void PauseSimulation() {
        if (isSimulating) {
            isSimulating = false;
            if (simulationCoroutine != null) {
                StopCoroutine(simulationCoroutine);
                simulationCoroutine = null;
            }
            UpdateGameText();
        }
    }

    IEnumerator SimulationRoutine() {
        while (isSimulating && gamePhase == GamePhase.Simulation && !isDestroying) {
            CalculateNextGeneration();
            ++currentGeneration;
            CalculateScores();
            CheckGameEnd();
            UpdateGameText();
            yield return new WaitForSeconds(simulationDelay);
        }
    }

    void CalculateNextGeneration() {
        if (isDestroying) return;

        Cell.CellColor[,] nextColors = new Cell.CellColor[gridWidth, gridHeight];
        bool[,] nextAlive = new bool[gridWidth, gridHeight];

        for (int y = 0; y < gridHeight; ++y) {
            for (int x = 0; x < gridWidth; ++x) {
                int aliveNeighbors = CountAliveNeighbors(x, y);
                Cell.CellColor majorityColor = GetMajorityColor(x, y);
                bool currentlyAlive = grid[x, y].isAlive;

                if (currentlyAlive && (aliveNeighbors == 2 || aliveNeighbors == 3)) {
                    nextAlive[x, y] = true;
                    nextColors[x, y] = grid[x, y].color;
                } else if (!currentlyAlive && aliveNeighbors == 3) {
                    nextAlive[x, y] = true;
                    nextColors[x, y] = majorityColor;
                } else {
                    nextAlive[x, y] = false;
                    nextColors[x, y] = Cell.CellColor.Dead;
                }
            }
        }

        for (int y = 0; y < gridHeight; ++y) {
            for (int x = 0; x < gridWidth; ++x) {
                grid[x, y].SetAlive(nextAlive[x, y], nextColors[x, y]);
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
                if (neighborX >= 0 && neighborX < gridWidth && neighborY >= 0 && neighborY < gridHeight) {
                    if (grid[neighborX, neighborY].isAlive) {
                        ++count;
                    }
                }
            }
        }
        return count;
    }

    Cell.CellColor GetMajorityColor(int x, int y) {
        int blueCount = 0;
        int redCount = 0;
        
        for (int i = -1; i <= 1; ++i) {
            for (int j = -1; j <= 1; ++j) {
                if (i != 0 && j != 0) continue;
                int neighborX = x + i;
                int neighborY = y + j;
                if (neighborX >= 0 && neighborX < gridWidth && neighborY >= 0 && neighborY < gridHeight) {
                    Cell cell = grid[neighborX, neighborY];
                    if (cell.isAlive) {
                        if (cell.color == Cell.CellColor.Blue) {
                            ++blueCount;
                        } else if (cell.color == Cell.CellColor.Red) {
                            ++redCount;
                        }
                    }
                }
            }
        }
        
        return blueCount >= redCount ? Cell.CellColor.Blue : Cell.CellColor.Red;
    }

    void CheckGameEnd() {
        if (currentGeneration >= totalGenerations) {
            gamePhase = GamePhase.GameOver;
            isSimulating = false;
            if (simulationCoroutine != null) {
                StopCoroutine(simulationCoroutine);
                simulationCoroutine = null;
            }
            ShowResultPanel();
        }
    }

    void UpdateGameText() {
        if (blueScoreText != null) {
            blueScoreText.text = $"Синие: {blueScore}";
        }
        if (redScoreText != null) {
            redScoreText.text = $"Красные: {redScore}";
        }

        if (gameStateText != null) {
            switch (gamePhase) {
                case GamePhase.Setup:
                    int remainingBlue = initialCellsPerPlayer - blueCellsPlaced;
                    int remainingRed = initialCellsPerPlayer - redCellsPlaced;
                    
                    if (blueCellsPlaced < initialCellsPerPlayer) {
                        gameStateText.text = $"Расстановка: СИНИЕ \n({remainingBlue} осталось)";
                    } else if (redCellsPlaced < initialCellsPerPlayer) {
                        gameStateText.text = $"Расстановка: КРАСНЫЕ \n({remainingRed} осталось)";
                    } else {
                        gameStateText.text = "Расстановка завершена! Нажмите СТАРТ";
                    }
                    break;
                case GamePhase.Simulation:
                    gameStateText.text = isSimulating ? 
                        $"Симуляция... Поколение: \n{currentGeneration} / {totalGenerations}" : 
                        "Симуляция на паузе";
                    break;
                case GamePhase.GameOver:
                    gameStateText.text = "ИГРА ОКОНЧЕНА!";
                    break;
            }
        }
        
        UpdateSpeedDisplay();
    }

    // Сброс игры
    public void ResetGame() {
        PauseSimulation();
        gamePhase = GamePhase.Setup;
        blueScore = 0;
        redScore = 0;
        currentGeneration = 0;
        blueCellsPlaced = 0;
        redCellsPlaced = 0;
        isBlueTurn = true;
        currentPattern = null;
        isPlacingPattern = false;

        HideResultPanel();

        InitializeGrid();

        for (int y = 0; y < gridHeight; ++y) {
            for (int x = 0; x < gridWidth; ++x) {
                grid[x, y].SetAlive(false, Cell.CellColor.Dead);
            }
        }

        CalculateScores();
        UpdateGameText();
    }

    public void StopAllCellAnimations() {
        if (grid == null) return;
        foreach (Cell cell in grid) {
            if (cell != null) {
                cell.StopAnimations();
            }
        }
    }

    public void ForceResetAllCells()
    {
        if (grid == null) return;
        foreach (Cell cell in grid)
        {
            if (cell != null)
            {
                cell.ForceResetState();
            }
        }
    }

    //Методы для правил
    public void ShowRules()
    {
        if (rulesPanel != null)
        {
            rulesPanel.SetActive(true);
            PauseSimulation();
        }
    }
    
    public void HideRules() {
        if (rulesPanel != null) {
            rulesPanel.SetActive(false);
        }
    }
}
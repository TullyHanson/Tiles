using UnityEngine;
using System.Collections;

public class Grid : MonoBehaviour {

    static public Grid gameGrid;
    public Tile tilePrefab;
    // Controls position of all Tiles
    public Tile[,] tileArray = new Tile[5, 13];
    public int gridWidth = 5, gridHeight = 12;
    public float distanceBetweenTiles = 0.5f; 
    public float gameplayXStart = 0.6f;
    public float currentHeight;

    // Counting semaphore used to determine if tiles are still falling
    public static int fallInProgressInt = 0;
    // Binary semaphore to control creation of new tiles
    public static bool pushInProgress = false;

    void Start ()
    {
        if(!gameGrid)
            // Get a reference to this script
            gameGrid = this; 
        else
        {
            Debug.LogError("Only one instance of Grid is allowed.");
            return;
        }

        // Set the distance given the current scale 
        distanceBetweenTiles = tilePrefab.transform.localScale.x;
        transform.position = new Vector3(gameplayXStart, 0, 0.0f);
        currentHeight = 0;

        // Begin to push tiles up from the bottom of the screen
        StartCoroutine(CreateTiles());
	}

    /* 
    Stub method that is called if a recently rotated tile is about to be destroyed.
    Calls this instead of the "DestroyTiles" coroutine, since destroyed objects can't have coroutines running
    Plus, we want the gameGrid to handle the majority of tileArray access 
    */
    public void UpdateGameGrid(ArrayList pointsToCheckForFall, ArrayList tilesToDestroy)
    {
        StartCoroutine(DestroyTiles(tilesToDestroy));
        StartCoroutine(_TellTilesToFall(pointsToCheckForFall));
    }

    // Destroy the gameobject and the reference in the tileArray for each Tile
    IEnumerator DestroyTiles(ArrayList tilesToDestroy)
    {
        // Wait until the tile creation is done before we destroy the tiles
        while (pushInProgress)
            yield return new WaitForSeconds(1.0f);
        
        // Play the destroy sound
        GetComponent<AudioSource>().Play();

        foreach (Tile thisTile in tilesToDestroy)
        {
            tileArray[thisTile.arrayPosX, thisTile.arrayPosY] = null;
            Destroy(thisTile.gameObject);
        }
        yield return null;
    }

    // Coroutine that tells tiles above recently destroyed Tiles to fall down into their place
    IEnumerator _TellTilesToFall(ArrayList pointsToCheckForFall)
    {
        // Wait until the tile creation is done and no other tiles are falling
        while (pushInProgress && fallInProgressInt == 0)
            yield return new WaitForSeconds(1.0f);
        
        ArrayList visitedXPositions = new ArrayList();
        foreach (Vector2 currentPosVector in pointsToCheckForFall)
        {
            // If we haven't visited this column yet (We don't want to ask blocks to fall twice)
            if (!visitedXPositions.Contains(currentPosVector.x))
            {
                visitedXPositions.Add(currentPosVector.x);
                int currentXPos = (int)currentPosVector.x;
                int currentYPos = (int)currentPosVector.y;
                
                // Start at the currentYPos, and iterate upwards to all tiles in the array
                for (int incrementalY = currentYPos; incrementalY < gridHeight; incrementalY++)
                {
                    // Make sure testX and incrementalY are within the grid bounds
                    if ((currentXPos >= 0 && currentXPos <= gridWidth - 1) &&
                    (incrementalY >= 0 && incrementalY <= gridHeight - 1))
                    {
                        // Assuming tile still exists, tell it to fall
                        if (tileArray[currentXPos, incrementalY] != null)
                            tileArray[currentXPos, incrementalY].StartCoroutine(tileArray[currentXPos, incrementalY].Fall());
                    }
                }
            }
        }
        yield return null;
    }

    // Repeatedly create and push tiles up from the bottom of the screen
    IEnumerator CreateTiles()
    {
        while (currentHeight < gridHeight)
        {
            // Set binary semaphore to prevent any more tile deletion/falls
            pushInProgress = true;
            // Wait until all current tile falling finishes
            while (fallInProgressInt != 0)
                yield return new WaitForEndOfFrame();

            // Iterate over all columns and rows
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = gridHeight - 2; y > 0; y--)
                {
                    if (tileArray[x, y] != null)
                    {
                        // Increase the arrayPosY variable of this current Tile
                        tileArray[x, y].arrayPosY++;
                        // Move this tile up one in the tileArray and set its previous position to null
                        tileArray[x, y + 1] = tileArray[x, y];
                        tileArray[x, y] = null;
                        // Tell this tile to move upwards
                        tileArray[x, y + 1].StartCoroutine(tileArray[x, y + 1].ShiftUp());
                    }
                }
                // Create a new tile at the appropriate position, and assign its arrayPosX/arrayPosY position
                tileArray[x,1] = Instantiate(tilePrefab, new Vector3(transform.position.x + x * distanceBetweenTiles, transform.position.y + -1 * distanceBetweenTiles, 0.0f), Quaternion.identity) as Tile;
                tileArray[x, 1].arrayPosX = x;
                tileArray[x, 1].arrayPosY = 1;
            }

            // Set binary semaphore to signal tile creation is finished
            pushInProgress = false;
            yield return new WaitForSeconds(3.0f);
        }
    }
}

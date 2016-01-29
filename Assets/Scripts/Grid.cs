using UnityEngine;
using System.Collections;

public class Grid : MonoBehaviour {

    static public Grid gameGrid;
    public Tile tilePrefab;
    public int gridWidth = 5, gridHeight = 12;
    public float distanceBetweenTiles = 0.5f; //Spacing between tiless
    public float gameplayXStart = 0.6f;
    public float currentHeight;
    public Tile[,] tileArray = new Tile[5, 13];



    public static int fallInProgressInt = 0;
    public static bool pushInProgress = false;

    // Use this for initialization
    void Start () {

        if(!gameGrid)
        {
            gameGrid = this; // Get a reference to this script, which is a static variable so it's used as a singleton
        }
        else
        {
            Debug.LogError("Only one instance of Grid is allowed.");
            return;
        }

        distanceBetweenTiles = tilePrefab.transform.localScale.x;
        transform.position = new Vector3(gameplayXStart, 0, 0.0f);
        currentHeight = 0;

        // Begin to push tiles up from the bottom of the screen
        StartCoroutine(CreateTiles());
	}
	
	// Update is called once per frame
	void Update () {
	}

    // Stub method that is called if a recently rotated tile(s) is about to be destroyed.
    // Calls this instead of the coroutine, since destroyed objects can't have coroutines running
    // Plus, we want the gameGrid to handle all tileArray access
    public void UpdateGameGrid(ArrayList pointsToCheckForFall, ArrayList tilesToDestroy)
    {
        StartCoroutine(DestroyTiles(tilesToDestroy));
        StartCoroutine(_TellTilesToFall(pointsToCheckForFall));
    }

    IEnumerator DestroyTiles(ArrayList tilesToDestroy)
    {
        print("Initial call to destroy...");
        while (pushInProgress)
            yield return new WaitForSeconds(1.0f);

        print("Through the wait...");
        GetComponent<AudioSource>().Play();
        //Can maybe do something fancy here....
        // May need to convert it to a coroutine so I can add a delay with respect to the tile creating
        foreach (Tile thisTile in tilesToDestroy)
        {
            tileArray[thisTile.arrayPosX, thisTile.arrayPosY] = null;
            Destroy(thisTile.gameObject);
            print("Destroyed");
        }
        yield return null;
    }

    // Coroutine that tells tiles above recently destroyed ones to fall down into their place. 
    IEnumerator _TellTilesToFall(ArrayList pointsToCheckForFall)
    {
        while (pushInProgress && fallInProgressInt == 0)
        {
            print("Wait for push to finish to fall...");
            yield return new WaitForSeconds(1.0f);
        }        
        // DEPENDS ON UNITY 3 OR HIGHER!!! MAY CAUSE AN ERROR
        ArrayList visitedX = new ArrayList();
        foreach (Vector2 currentArrayPos in pointsToCheckForFall)
        {
            // If we haven't visited this column yet (We don't want to ask blocks to fall twice)
            if (!visitedX.Contains(currentArrayPos.x))
            {
                visitedX.Add(currentArrayPos.x);
                int testX = (int)currentArrayPos.x;
                int testY = (int)currentArrayPos.y;
                
                for (int incrementalY = testY; incrementalY < gridHeight; incrementalY++)
                {
                    if ((testX >= 0 && testX <= gridWidth - 1) &&
                    (incrementalY >= 0 && incrementalY <= gridHeight - 1))
                    {
                        if (tileArray[testX, incrementalY] != null)
                        {
                            print("Fall request to block: " + testX + ", " + incrementalY);
                            tileArray[testX, incrementalY].StartCoroutine(tileArray[testX, incrementalY].Fall());
                        }
                    }
                }
            }
        }
        yield return null;
    }

    // Repeatedly push tiles up from the bottom of the screen
    IEnumerator CreateTiles()
    {
        yield return new WaitForSeconds(3.0f);

        while (currentHeight < gridHeight)
        {
            pushInProgress = true;
            
            //yield return new WaitForSeconds(5.0f);

            while (fallInProgressInt != 0)
            {
                print("Wait: " + fallInProgressInt);
                yield return new WaitForEndOfFrame();
            }

            print("PUSH STARTED");

            for (int x = 0; x < gridWidth; x++)
            {
                print("x: " + x);
                for (int y = gridHeight - 2; y > 0; y--)
                {
                    if (tileArray[x, y] != null)
                    {
                        tileArray[x, y].arrayPosY++;
                        tileArray[x, y + 1] = tileArray[x, y];
                        tileArray[x, y] = null;
                        tileArray[x, y + 1].StartCoroutine(tileArray[x, y + 1].ShiftUp());
                    }
                }
                tileArray[x,1] = Instantiate(tilePrefab, new Vector3(transform.position.x + x * distanceBetweenTiles, transform.position.y + -1 * distanceBetweenTiles, 0.0f), Quaternion.identity) as Tile;
                tileArray[x, 1].arrayPosX = x;
                tileArray[x, 1].arrayPosY = 1;
            }

            //yield return new WaitForEndOfFrame();

            pushInProgress = false;
            print("PUSH FINISHED");
            yield return new WaitForSeconds(3.0f);
        }
    }
}

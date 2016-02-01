using UnityEngine;
using System.Collections;

public class Tile : MonoBehaviour {

    public Camera cam;
    public float tileWidth;
    public float halfTileWidth;
    // Binary semaphores controlling Tile movement
    public bool isFalling = false;
    public bool isShiftingUp = false;

    public bool waitingToBeDeleted = false;

    // This tiles current position
    public int arrayPosX = 0;
    public int arrayPosY = 0;

    // Order defined as { Top, Right, Bottom, Left }
    // Holds the int representation of color for this tile
    public int[] colorArray = new int[4];

    // Holds the sprites that correspond to each color
    public Sprite[] availableSprites;

    void Start ()
    {
        if (cam == null)
            cam = Camera.main;
        tileWidth = transform.localScale.x;
        halfTileWidth = tileWidth / 2.0f;

        GenerateTileColor();

        /* Very first shift bringing the tile on screen. 
         This is the only upwards shift called by the tile itself,
         rest of them are controlled by the Grid. 
        */
        StartCoroutine(ShiftUp());
    }

    // Generates the random colors for a tile
    void GenerateTileColor()
    {
        // Pick an int representing a color for each part of the tile
        for (int i = 0; i < 4; i++)
            colorArray[i] = Random.Range(0, 4);
        
        SpriteRenderer[] childrenComps = this.GetComponentsInChildren<SpriteRenderer>();
        // Assign sprites based on the colors picked above
        for (int i = 1; i < 5; i++)
            childrenComps[i].sprite = availableSprites[colorArray[i-1]];
    }

    // Update is called once per frame
    void Update ()
    {
        // Add a listener for input from the player
        StartCoroutine(Rotate90DegreeCW_Desktop());
    }

    // Shift the tiles position up graphically
    public IEnumerator ShiftUp()
    {
        isShiftingUp = true;
        Vector3 target = new Vector3(transform.position.x, transform.position.y + tileWidth, transform.position.z);
        float speed = 1.0f;

        // Increase the tiles Y position over a given amount of time
        while (transform.position != target)
        {
            float step = speed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, target, step);
            yield return new WaitForEndOfFrame();
        }

        isShiftingUp = false;
        yield return null;
    }

    // Listens for player input, uses mobile input functions 
    IEnumerator Rotate90DegreeCW_Mobile()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch thisTouch = Input.GetTouch(i);

            // If the screen was tapped and this tile isn't already waiting to be deleted
            if (thisTouch.phase == TouchPhase.Began && !waitingToBeDeleted)
            {
                // Finds the position on the screen that was tapped
                Vector3 rawPosition = cam.ScreenToWorldPoint(thisTouch.rawPosition);
                Vector3 targetPosition = new Vector3(rawPosition.x, rawPosition.y, 0.0f);

                // If the tapped point lies within the bounds of this tile
                if (Mathf.Abs(targetPosition.x - transform.position.x) < halfTileWidth && Mathf.Abs(targetPosition.y - transform.position.y) < halfTileWidth)
                {
                    // Play rotation sound
                    GetComponent<AudioSource>().Play();
                    // Rotate 90 degrees clockwise
                    Vector3 oldRotation = transform.eulerAngles;
                    transform.eulerAngles = new Vector3(0.0f, 0.0f, oldRotation.z - 90.0f);

                    //Rotate the color array of this tile to match its new orientation
                    rotateColorArray();
                    // Check tiles adjacent to this tile to see if there are color matches
                    StartCoroutine(CheckAdjacent());
                }
            }
        }
        yield return null;
    }

    /*
    Listens for player input, uses desktop input functions
    Also works for mobile input as well.
    */
    IEnumerator Rotate90DegreeCW_Desktop()
    {
        // If the screen was clicked and this tile isn't already waiting to be deleted
        if (Input.GetMouseButtonDown(0) && !waitingToBeDeleted)
        {
            // Finds the position on the screen that was clicked
            Vector3 rawPosition = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 targetPosition = new Vector3(rawPosition.x, rawPosition.y, 0.0f);

            // If the clicked point lies within the bounds of this tile
            if (Mathf.Abs(targetPosition.x - transform.position.x) < halfTileWidth && Mathf.Abs(targetPosition.y - transform.position.y) < halfTileWidth)
            {
                // Play rotation sound
                GetComponent<AudioSource>().Play();
                // Rotate 90 degrees clockwise
                Vector3 oldRotation = transform.eulerAngles;
                transform.eulerAngles = new Vector3(0.0f, 0.0f, oldRotation.z - 90.0f);
                
                //Rotate the color array of this tile to match its new orientation
                rotateColorArray();
                // Check tiles adjacent to this tile to see if there are color matches
                StartCoroutine(CheckAdjacent());
            } 
        }
        yield return null;
    }

    // Checks the 4 adjacent positions around this Tile to look for color matches
    IEnumerator CheckAdjacent()
    {
        // Order the adjacency array in { Above, Right, Below, Left } to simplify the later loop
        Tile[] adjacentTiles = { null, null, null, null };

        // Fill tile array when the given positions are within the bounds of the game grid
        if (arrayPosX >= 0 && arrayPosY <= Grid.gameGrid.gridHeight - 2)
            adjacentTiles[0] = Grid.gameGrid.tileArray[arrayPosX, arrayPosY + 1];
        if (arrayPosX <= Grid.gameGrid.gridWidth - 2 && arrayPosY >= 0)
            adjacentTiles[1] = Grid.gameGrid.tileArray[arrayPosX + 1, arrayPosY];
        if (arrayPosX >= 0 && arrayPosY >= 1)
            adjacentTiles[2] = Grid.gameGrid.tileArray[arrayPosX, arrayPosY - 1];
        if (arrayPosX >= 1 && arrayPosY >= 0)
            adjacentTiles[3] = Grid.gameGrid.tileArray[arrayPosX - 1, arrayPosY];
        
        ArrayList pointsToCheckForFall = new ArrayList();
        ArrayList tilesToDestroy = new ArrayList();
        bool atleastOneDeleted = false;

        for (int i = 0; i < adjacentTiles.Length; i++)
        {
            // If there is an adjacent tile at this index (Comes into play at edges/corners/upper layer of tiles)
            if(adjacentTiles[i] != null)
            {
                /*
                Determines if the appropriate opposite inner sides of 2 adjacent tiles have the same color

                Example, given that we are tile X:
                _0_
                1X2
                _3_
                
                When checking to see if there is a match with:
                    Tile 0, we want to check the top color of X with the bottom of 0.
                    Tile 1, we want to check the left color of X with the right of 1.
                    Tile 2, we want to check the right color of X with the left of 2.
                    Tile 3, we want to check the bottom color of X with the top of 3.
                */
                if (colorArray[i] == adjacentTiles[i].colorArray[(i + 2) % 4])
                {
                    // An adjacent tile is set to be deleted, so we know we want to delete the middle tile later
                    atleastOneDeleted = true;
                    // Set this boolean value so future taps don't move this tile out of position
                    waitingToBeDeleted = true;

                    // Check to make sure we don't try to delete the same tile twice
                    if (!adjacentTiles[i].waitingToBeDeleted)
                    {
                        adjacentTiles[i].waitingToBeDeleted = true;
                        // Add the position of this tile to an array so we know to notify tiles above it to fall
                        pointsToCheckForFall.Add(new Vector2(adjacentTiles[i].arrayPosX, adjacentTiles[i].arrayPosY + 1));
                        // Add this tile to a list to be deleted
                        tilesToDestroy.Add(adjacentTiles[i]);
                    }
                }
            }
        }

        // There was a match, so this tile must be deleted as well
        if (atleastOneDeleted)
        {
            pointsToCheckForFall.Add(new Vector2(arrayPosX, arrayPosY + 1));
            tilesToDestroy.Add(this);
            // Notify the grid to update tile positions accordingly
            Grid.gameGrid.UpdateGameGrid(pointsToCheckForFall, tilesToDestroy);
        }
        yield return null;
    }

    public IEnumerator Fall()
    {
        // Wait until this tile is finished shifting upwards
        while (isShiftingUp)
            yield return new WaitForEndOfFrame();

        // Increment the semaphore to prevent further tile creation until this tile is done falling
        Grid.fallInProgressInt++;
        bool keepDropping = arrayPosY >= 2 ? true : false;

        // While this tile has room below to fall || the block directly below it is falling as well
        while (keepDropping && (Grid.gameGrid.tileArray[arrayPosX, arrayPosY - 1] == null || Grid.gameGrid.tileArray[arrayPosX, arrayPosY - 1].isFalling))
        {
            isFalling = true;

            // Find the desired location for this tile to fall to
            Vector3 target = new Vector3(transform.position.x, transform.position.y - tileWidth, transform.position.z);
            float speed = 2.5f;
            // Move its position slowly to make it look fluid
            while (transform.position.y > target.y)
            {
                float step = speed * Time.deltaTime;
                transform.position = Vector3.MoveTowards(transform.position, target, step);
                yield return new WaitForEndOfFrame();
            }
            arrayPosY = arrayPosY - 1;
            // Change its position in the tile array since it finished falling
            Grid.gameGrid.tileArray[arrayPosX, arrayPosY] = this;
            Grid.gameGrid.tileArray[arrayPosX, arrayPosY + 1] = null;
            
            keepDropping = arrayPosY >= 2 ? true : false;
            isFalling = false;
            
            yield return new WaitForEndOfFrame();
        }
        // Decrement the semaphore to allow more tiles to be created
        Grid.fallInProgressInt--;
        yield return null;
    }

    // Rotates the color array so it lines up with the colors represented on screen
    public void rotateColorArray()
    {
        //{Top, Right, Bottom, Left}
        int placeHolder = colorArray[3];
        // Slides whole array forward one index, wrapping around
        for(int i = colorArray.Length - 1; i > 0; i--)
            colorArray[i] = colorArray[i - 1];
        colorArray[0] = placeHolder;
    }
}

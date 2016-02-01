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
        StartCoroutine(Rotate90DegreeCW());
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


    IEnumerator Rotate90DegreeCW()
    {
    // MOBILE //     for (int i = 0; i < Input.touchCount; i++)
    // MOBILE //     {
    // MOBILE //         Touch thisTouch = Input.GetTouch(i);

            // MOBILE // if (thisTouch.phase == TouchPhase.Began)
            if(Input.GetMouseButtonDown(0))
            {
                if (!waitingToBeDeleted)
                {
                    // MOBILE // Vector3 rawPosition = cam.ScreenToWorldPoint(thisTouch.rawPosition);
                    Vector3 rawPosition = cam.ScreenToWorldPoint(Input.mousePosition);
                    Vector3 targetPosition = new Vector3(rawPosition.x, rawPosition.y, 0.0f);

                    if (Mathf.Abs(targetPosition.x - transform.position.x) < halfTileWidth && Mathf.Abs(targetPosition.y - transform.position.y) < halfTileWidth)
                    {
                        GetComponent<AudioSource>().Play();
                        Vector3 oldRotation = transform.eulerAngles;
                        transform.eulerAngles = new Vector3(0.0f, 0.0f, oldRotation.z - 90.0f);

                        rotateColorArray();

                        StartCoroutine(CheckAdjacent());
                    }
                }
            }
        // MOBILE // }
        yield return null;
    }

    IEnumerator CheckAdjacent()
    {
        // Order the adjacency array in { Above, Right, Below, Left }
        // Simplifies the for loop
        Tile[] adjacentTiles = { null, null, null, null };

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
            if(adjacentTiles[i] != null)
            {
                if (colorArray[i] == adjacentTiles[i].colorArray[(i + 2) % 4])
                {
                    atleastOneDeleted = true;
                    waitingToBeDeleted = true;

                    if (!adjacentTiles[i].waitingToBeDeleted)
                    {
                        adjacentTiles[i].waitingToBeDeleted = true;
                        pointsToCheckForFall.Add(new Vector2(adjacentTiles[i].arrayPosX, adjacentTiles[i].arrayPosY + 1));
                        tilesToDestroy.Add(adjacentTiles[i]);
                    }
                }
            }
        }

        if (atleastOneDeleted)
        {
            pointsToCheckForFall.Add(new Vector2(arrayPosX, arrayPosY + 1));

            tilesToDestroy.Add(this);
            
            Grid.gameGrid.UpdateGameGrid(pointsToCheckForFall, tilesToDestroy);
        }
        yield return null;
    }

    public IEnumerator Fall()
    {
        while (isShiftingUp)
            yield return new WaitForEndOfFrame();

        Grid.fallInProgressInt++;
        bool keepDropping = arrayPosY >= 2 ? true : false;

        // While this tile has room below to fall or the block directly below me is falling as well
        while (keepDropping && (Grid.gameGrid.tileArray[arrayPosX, arrayPosY - 1] == null || Grid.gameGrid.tileArray[arrayPosX, arrayPosY - 1].isFalling))
        {
            isFalling = true;

            //Fall
            Vector3 target = new Vector3(transform.position.x, transform.position.y - tileWidth, transform.position.z);
            float speed = 2.5f;
            print("Start falling: " + this.GetInstanceID());
            while (transform.position.y > target.y)
            {
                print(transform.position.y + " > " + target.y);
                float step = speed * Time.deltaTime;
                transform.position = Vector3.MoveTowards(transform.position, target, step);
                yield return new WaitForEndOfFrame();
            }
            print("Finished: " + transform.position.y);
            arrayPosY = arrayPosY - 1;
            Grid.gameGrid.tileArray[arrayPosX, arrayPosY] = this;
            Grid.gameGrid.tileArray[arrayPosX, arrayPosY + 1] = null;
            
            keepDropping = arrayPosY >= 2 ? true : false;
            isFalling = false;
            
            yield return new WaitForEndOfFrame();
        }
        Grid.fallInProgressInt--;
        yield return null;
    }

    public void rotateColorArray()
    {
        //{ Top, Right, Bottom, Left }
        int placeHolder = colorArray[3];
        for(int i = colorArray.Length - 1; i > 0; i--)
            colorArray[i] = colorArray[i - 1];
        colorArray[0] = placeHolder;
    }
}

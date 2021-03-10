using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using System.Drawing;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Forms;
// App ----------------------------------------

namespace GeneratorApp
{
  public class App
  {
    static void Main(string[] args)
    {
      Console.WriteLine("Do you want to see the whole process? (Y/N):");
      string showcase = Console.ReadLine();
      bool continuous_save = false;
      if (showcase.ToUpper() == "Y")
      {
        Console.WriteLine("Do you want to save the whole process? (Y/N):");
        string continuous_save_text = Console.ReadLine();
        if (continuous_save_text.ToUpper() == "Y") { continuous_save = true; }
      }
      TerrainGenerator tg = new TerrainGenerator();
      tg.continuous_save = continuous_save;
      Form form = new Form();
      form.Width = 400;
      form.Height = 400;
      if (showcase.ToUpper() == "Y") 
      {
        tg.showcase = true;
        tg.form = form;
        Task.Run(() => {
          Application.Run(tg.form);
          tg.form.BringToFront();
          tg.form.Activate();
        });
        tg.form.Text = "Generation Showcase";
      }
      while (!tg.finished) 
      {
        try { tg.Update(); } 
        catch(Exception e) { Console.WriteLine(e.ToString()); }
        if (tg.finished) 
        {
          Console.WriteLine("FINISHED");
          Console.WriteLine("Do you want to repeat it? (Y/N):");
          string repeat = Console.ReadLine();
          if (repeat.ToUpper() == "Y")
          {
            tg = new TerrainGenerator();
            tg.continuous_save = continuous_save;
            if (showcase.ToUpper() == "Y")  
            { 
              tg.showcase = true; 
              tg.form = form;
            }
        } }
      }
} } }

// other classes ------------------------------
  class TerrainGenerator 
  {
    //---------- common vars --------public----
      public Form form;
      public int maxCircleCount = 30;
      public int maxDrillBaseN = 50;
      public bool debugging = true;
      public bool useSeed = false;
      public int seed = 42;
      public bool showcase = false;
      public bool _save = true;
      public bool continuous_save = false;
      public bool finished = false;
      public int auxValue = 0,
      generation_phase = 0,
      roundNumber = 0,
      size = 128,
      updateCount = 0;
      public int[,] alphas; // indexed as y,x
      private int multiplyingSteps = 3;
      private int alreadyMultiplied = 0;
      private int sizeMultiplier = 2;
      private int minDesiredLandSmallSize = 2000;
      private int maxDesiredLandSmallSize = 4000;
      private List<Vector2Int> shapePoints = new List<Vector2Int>();
      private int maxSmoothSteps = 1;
      private int maxResizedSmoothSteps = 4;
      private int maxBleedSteps = 6;
      private List<Vector2Int> bleedPoints = new List<Vector2Int>();
      private int numberOfTimesReset = 0;
      private Random rnd = new Random();
    // main -----------------------------------
      void _Start() 
      {
        if (useSeed) { rnd = new Random(seed); }
        alphas = new int[size,size];
        for(int i=0;i<size;i++) { for(int j=0;j<size;j++) { alphas[j,i] = 0; } }
      }

      public void Update()
      {
        if (!finished)
        {
          updateCount++;
          switch(generation_phase)
          {
            case 0:  // Starting 
              CallDebug("Starting TerrainGenerator.cs");
              _Start();
              generation_phase++;
              break;
            case 1:  // ** Generate Circles 
              CallDebug("generating initial circles");
              if (auxValue<maxCircleCount)
              {
                GenerateCircle();
                auxValue+=1;
              } else { generation_phase++; auxValue=0; }
              break;
            case 8:  // Smooth Island Borders
            case 2:  // Smooth Island Borders
              CallDebug("smoothing island borders");
              if (auxValue<maxSmoothSteps)
              {
                SmoothIslandBorders(3f,1f,0.5f,1.0f,127); // (3ij + ipj + imj + ijp + ijm + (ipjp + imjp + ipjm + imjm)/2)/9 > 0.5f -> 1.0f; <=0.5f -> 0.0f
                auxValue++;
              } else { generation_phase++; auxValue=0; }
              break;
            case 9:  // Separate Islands
            case 3:  // Separate Islands
              CallDebug("separating islands");
              auxValue = SeparateIslands();
              generation_phase++;
              break;
            case 10: // Remove Extra Islands
            case 4:  // Remove Extra Islands
              CallDebug("removing extra islands");
              RemoveExtraIslands(auxValue);
              auxValue = 0;
              generation_phase++;
              break;
            case 5:  // Bleed Island 
              CallDebug("bleeding island border");
              if (auxValue < maxBleedSteps)
              {
                auxValue++;
                BleedIslandWithDistanceToIsland(1f,1f,1f,auxValue,1f/3f);
              } else { generation_phase++; auxValue=0; }
              break;
            case 6:  // Drill Island Centered in Bleeds  
              CallDebug("drilling island");
              if (auxValue<maxDrillBaseN)
              {
                GenerateDrillCircle();
                auxValue++;
              } else { generation_phase++; auxValue=0; }
              break;
            case 7:  // Remove Bleed
              CallDebug("removing bleeded borders");
              for(int i=0; i<size; i++) { for(int j=0; j<size; j++) {
                if (alphas[j,i] != 255) { alphas[j,i] = 0; }
              } }
              bleedPoints = new List<Vector2Int>();
              generation_phase++;
              break;
            case 11: // Checking if size is of a desirable value
              CallDebug("Checking if size is of a desirable value");
              int landSmallSize = 0;
              for(int i=0; i<size; i++) { for(int j=0; j<size; j++) {
                if (alphas[j,i] == 255) { landSmallSize++; }
              } }
              if (debugging) { Console.WriteLine("Size before resizing texture: "+landSmallSize); }
              if (landSmallSize<=minDesiredLandSmallSize || landSmallSize>=maxDesiredLandSmallSize)
              { GenerationReset(); numberOfTimesReset++; } 
              else { generation_phase++; }
              break;
            case 12: // BLEED FOR SMOOTHING DATA 
              if (debugging) { Console.WriteLine("Case: "+generation_phase+"."+alreadyMultiplied+", bleeding island border for smoothing data, round:"+(roundNumber+1).ToString()); }
              BleedIslandSimple(0, 255, 100); // bleeding outer edge
              BleedIslandSimple(0, 100, 100); // bleeding outer edge
              BleedIslandSimple(255, 100, 200); // bleeding inner edge
              BleedIslandSimple(255, 200, 200); // bleeding inner edge
              generation_phase++;
              break;
            case 13: // LOAD SHAPE DATA INTO LIST
              CallDebug("loading shape data into a list");
              LoadShapeDataIntoList();
              generation_phase++;
              break;
            case 14: // LOAD LIST DATA INTO RESIZED SHAPE
              CallDebug("loading list data into resized shape");
              LoadListDataIntoNewSizedShape();
              generation_phase++;
              break;
            case 15: // ** SMOOTH RESIZED SHAPE BORDERS 
              CallDebug("smoothing island's borders");
              if (auxValue<maxResizedSmoothSteps)
              {
                if (alreadyMultiplied==2)
                {
                  BoxBlur(sizeMultiplier+2, 130);
                  auxValue++;
                } else { auxValue = maxResizedSmoothSteps; }
              } else 
              {
                generation_phase++;
                auxValue = 0;
                alreadyMultiplied++;
                if(alreadyMultiplied<multiplyingSteps)
                {
                  generation_phase = 12;
                  DebugMultiplications();
                }
              }
              break;
            case 16: // saving generated TG
              if (_save)
              {
                CallDebug("saving generated isle into .png file");
                Save();
                _save = false;
              }
              generation_phase++;
              break;
            case 17: // Finished
              CallDebug("Finished Island Shape Generation");
              finished = true;
              numberOfTimesReset = 0;
              generation_phase++;
              break;
            default:
              CallDebug("Default - Last step - finished TerrainGenerator.cs");
              break;
          }
          if (showcase) {
            Showcase();
            System.Threading.Thread.Sleep(50);
          }
        } else 
        {
          if (debugging) { Console.WriteLine("Destroying this object's TerrainGenerator.cs script"); }
          finalCountdown();
        }
      }
    // private methods ------------------------
      //         unique -----------------------
        void BleedIslandSimple(int target, int neighbourValue, int newValue)
        { 
          List<Vector2Int> auxList = new List<Vector2Int>();
          for (int i=0; i<size; i++) { for (int j=0; j<size; j++) {
              if (alphas[j,i] == target)
              {
                Vector2Int[] neighbours = Neighbours(new Vector2Int(i,j), size);
                List<int> neighbourValues = new List<int>();
                foreach (Vector2Int neighbour in neighbours){
                  neighbourValues.Add(alphas[neighbour.y,neighbour.x]);
                }
                if (neighbourValues.Contains(neighbourValue)) { auxList.Add(new Vector2Int(i,j)); }
              }
          } }
          foreach (Vector2Int point in auxList) { alphas[point.y,point.x] = newValue; }
        }
        void BleedIslandWithDistanceToIsland(float a, float b, float c, int d, float e)
        { 
          // similar to smooth but with some different rules: > -> >= ; ignores recently created values; uses (255-d)/255f as new alpha value
          // used in code as: BleedIsland(1f,1f,1f,auxValue,1f/3f); 
          d = Convert.ToInt32((float)d*(float)size/128f);
          for(int i=0;i<size;i++) { for(int j=0;j<size;j++) {
              if (alphas[j,i] == 0)
              {
                int i_p = Math.Min (i + 1, size - 1);
                int j_p = Math.Min (j + 1, size - 1);
                int i_m = Math.Max(i-1,0);
                int j_m = Math.Max(j-1,0);
                float newValue = (a*(alphas[j  ,i  ] > 255-d ? 1f : 0f )+ 
                                  b*(alphas[j  ,i_m] > 255-d ? 1f : 0f ) + 
                                  b*(alphas[j  ,i_p] > 255-d ? 1f : 0f ) + 
                                  b*(alphas[j_m,i  ] > 255-d ? 1f : 0f ) + 
                                  b*(alphas[j_p,i  ] > 255-d ? 1f : 0f ) + 
                                  c*(alphas[j_p,i_p] > 255-d ? 1f : 0f ) + 
                                  c*(alphas[j_m,i_m ]> 255-d ? 1f : 0f ) + 
                                  c*(alphas[j_p,i_m] > 255-d ? 1f : 0f ) + 
                                  c*(alphas[j_m,i_p] > 255-d ? 1f : 0f ))/(a+4*b+4*c);
                if (newValue >= e)
                {
                  alphas[j,i] = 255-d;
                  bleedPoints.Add(new Vector2Int(i,j));
                } else { alphas[j,i] = 0; }
          } } }
        }
        void BoxBlur(int k, int treshold)
        {
          List<int[]> auxList = new List<int[]>();
          foreach (Vector2Int point in bleedPoints)
          {
            float boxValue = 0f;
            for (int i=-k; i<=k; i++) { for (int j=-k; j<=k; j++) {
                if (!(point.x+i<0 || point.y+i>=size || point.y+j<0 || point.y+j>=size))
                {
                  float kernel = 1f/((2*k+1)*(2*k+1));
                  boxValue+=(float)alphas[point.y+j,point.x+i]*kernel;
                }
            } }
            int intBoxValue = 0;
            if (boxValue>(float)treshold) { intBoxValue= 255; }
            auxList.Add(new int[3]{point.x,point.y,intBoxValue});
          }
          foreach (int[] info in auxList)
          {
            alphas[info[1],info[0]] = info[2];
          }
        }
        void DebugMultiplications()
        {
          string auxText = "th";
          switch(alreadyMultiplied)
          {
            case 1:
              auxText = "st";
              break;
            case 2:
              auxText = "nd";
              break;
            case 3:
              auxText = "rd";
              break;
          }
          if (debugging) { Console.WriteLine("rebooting to step="+generation_phase+", "+alreadyMultiplied+auxText+" time"); }
        }
        void GenerationReset()
        {
          roundNumber++;
          if (debugging) { Console.WriteLine("Restarting TerrainGenerator.cs"); }
          size = 128;
          alreadyMultiplied = 0;
          shapePoints = new List<Vector2Int>();
          generation_phase = 1;
          auxValue = 0;
          finished = false;
          bleedPoints = new List<Vector2Int>();
          alphas = new int[size,size];
          for(int i=0;i<size;i++) { for(int j=0;j<size;j++) { alphas[j,i] = 0; } }
        }
        void GenerateCircle()
        {  
          Vector2Int point = new Vector2Int();
          float distanceFromTextureCenter = (0.8f* (float)rnd.NextDouble() + 0.2f)*size/2f;
          float initialPointDirection =  (float)rnd.NextDouble()*2*(float)Math.PI;
          point.x = Convert.ToInt32(size/2 + distanceFromTextureCenter*Math.Cos(initialPointDirection));
          point.y = Convert.ToInt32(size/2 + distanceFromTextureCenter*Math.Sin(initialPointDirection));
          int r1 = Convert.ToInt32(0.5f*((3.0f+2.0f*((float)size/128f))*rnd.NextDouble() + 2.0f*((float)size/128f))*((float)size/distanceFromTextureCenter));
          int minPerturbationRadius = Math.Max(1 ,r1 - Convert.ToInt32(1.0f + 3.0f*rnd.NextDouble()));
          for(int i=-r1; i<=r1; i++) { for(int j=-r1; j<=r1; j++) { if (i*i+j*j<=r1*r1) {
                Vector2Int newPoint = new Vector2Int(
                    Math.Max(0,Math.Min(size-1,point.x+i)),
                    Math.Max(0,Math.Min(size-1,point.y+j)));
                if (i*i+j*j<=minPerturbationRadius*minPerturbationRadius) 
                { alphas[newPoint.y,newPoint.x] = 255; } 
                else { if (rnd.NextDouble()<0.26f) 
                { alphas[newPoint.y,newPoint.x] = 255; } }
          } } }
        }
        void GenerateDrillCircle()
        {
          // should it be perimeter dependant?
          int pointIndex = rnd.Next(0,bleedPoints.Count);
          Vector2Int point = bleedPoints[pointIndex];
          bleedPoints.RemoveAt(pointIndex);
          int radius = 255 - alphas[point.y,point.x];
          if (radius == 254) { radius = 1; }
          radius = radius + Convert.ToInt32((1f + 2f*rnd.NextDouble())*(float)size/128f);
          int minPerturbationRadius = Math.Min(radius-1, radius - Convert.ToInt32(3.0f*rnd.NextDouble()));
          int alphaValue = 255 - Math.Max(1, 1*Convert.ToInt32((float)size/128f));
          for(int i=-radius; i<=radius; i++) { for(int j=-radius; j<=radius; j++) {
              int currentRadius = Convert.ToInt32(Math.Sqrt(i*i+j*j));
              if(currentRadius<=radius)
              {
                Vector2Int newPoint = new Vector2Int(
                    Math.Max(0,Math.Min(size-1,point.x+i)),
                    Math.Max(0,Math.Min(size-1,point.y+j)));
                if (currentRadius<=minPerturbationRadius) 
                { 
                  if (alphas[newPoint.y, newPoint.x] == 255) 
                  { alphas[newPoint.y, newPoint.x] = 1; } 
                } else { if (rnd.NextDouble()<0.2f) { 
                  if (alphas[newPoint.y, newPoint.x] == 255) 
                  { alphas[newPoint.y, newPoint.x] = 1; } 
                } }
          } } }
        }
        void LoadListDataIntoNewSizedShape()
        {
          size = sizeMultiplier*size;
          alphas = new int[size,size];
          for(int i=0;i<size;i++) { for(int j=0;j<size;j++) { alphas[j,i] = 0; } }
          List<Vector2Int> auxList = new List<Vector2Int>();
          foreach(Vector2Int point in shapePoints) { for (int i=0; i<sizeMultiplier; i++) { for (int j=0; j<sizeMultiplier; j++) 
          { alphas[point.y*sizeMultiplier+j,point.x*sizeMultiplier+i] = 255; } } }
          foreach(Vector2Int point in bleedPoints) { for (int i=0; i<sizeMultiplier; i++) { for (int j=0; j<sizeMultiplier; j++) 
          { auxList.Add(new Vector2Int(point.x*sizeMultiplier+i,point.y*sizeMultiplier+j)); } } }
          bleedPoints = auxList;
          shapePoints =  new List<Vector2Int>();
        }
        void LoadShapeDataIntoList()
        {
          for (int i=0; i<size; i++) { for (int j=0; j<size; j++) {
              if (alphas[j,i] == 255) { shapePoints.Add(new Vector2Int(i,j)); } 
              else 
              {
                if (alphas[j,i] == 200) { shapePoints.Add(new Vector2Int(i,j));  bleedPoints.Add(new Vector2Int(i,j)); } 
                else { if (alphas[j,i] == 100) { bleedPoints.Add(new Vector2Int(i,j)); } }
              }
          } }
        }
        static Vector2Int[] Neighbours(Vector2Int point, int size)
        {
          Vector2Int[] neighbours = new Vector2Int[8]
          {
            new Vector2Int(Math.Min(point.x+1,size-1),point.y),                      // + 0
            new Vector2Int(Math.Max(point.x-1,0),point.y),                           // - 0
            new Vector2Int(point.x,Math.Min(point.y+1,size-1)),                      // 0 +
            new Vector2Int(point.x,Math.Max(point.y-1,0)),                           // 0 -
            new Vector2Int(Math.Min(point.x+1,size-1),Math.Min(point.y+1,size-1)),  // + +
            new Vector2Int(Math.Max(point.x-1,0),Math.Max(point.y-1,0)),            // - -
            new Vector2Int(Math.Min(point.x+1,size-1),Math.Max(point.y-1,0)),       // + -
            new Vector2Int(Math.Max(point.x-1,0),Math.Min(point.y+1,size-1))        // - +
          };
          return neighbours;
        }
        void RemoveExtraIslands(int biggerIslandAlpha)
        {
          for(int i=0;i<size;i++) { for(int j=0;j<size;j++) {
              if (alphas[j,i]!=biggerIslandAlpha)
              { alphas[j,i] = 0; }
              else 
              { alphas[j,i] = 255; }
        } } }
        int SeparateIslands() // return biggest island's alpha value
        {
          List<int> islandAlphas = new List<int>(); // float
          List<int> islandSizes = new List<int>(); // ushort
          for (int i=254; i>0; i--)
          {
            islandAlphas.Add(i);
            islandSizes.Add(0);
          }
          int listPointer = 0;
          for(int i=0;i<size;i++) { for(int j=0;j<size;j++) {// j goes up i goes right
              if (alphas[j,i] == 255)
              {
                int neighbourCount = 0;
                int firstNeighbour = 0;
                int i_m = Math.Max(i-1,0);
                int j_m = Math.Max(j-1,0);
                int[] neighbourValues = new int[2] {alphas[j,i_m],alphas[j_m,i]};
                foreach (int value in neighbourValues)
                {
                  List<int> auxList = islandAlphas.GetRange(0,listPointer);
                  if (auxList.Contains(value))
                  {
                    neighbourCount++;
                    if (firstNeighbour==0) { firstNeighbour=value; }
                } }
                switch (neighbourCount)
                {
                  case 0: 
                    islandSizes[listPointer]++;
                    alphas[j,i] = islandAlphas[listPointer];
                    listPointer++;
                    break;
                  case 1: 
                    int auxListPointer = islandAlphas.IndexOf(firstNeighbour);
                    islandSizes[auxListPointer]++;
                    alphas[j,i] = firstNeighbour;
                    break;
                  case 2:
                    int auxListPointer_0 = islandAlphas.IndexOf(neighbourValues[0]);
                    int auxListPointer_1 = islandAlphas.IndexOf(neighbourValues[1]);
                    islandSizes[auxListPointer_0]++;
                    alphas[j,i] = neighbourValues[0];
                    if (neighbourValues[0]!=neighbourValues[1])
                    { for (int i2=0;i2<=i;i2++) { for (int j2=0;j2<size;j2++) {
                          if (alphas[j2,i2]==neighbourValues[1])
                          {
                            islandSizes[auxListPointer_0]++;
                            islandSizes[auxListPointer_1]--;
                            if (islandSizes[auxListPointer_1]==0)
                            {
                              islandSizes.Add(0);
                              islandAlphas.Add(islandAlphas[auxListPointer_1]);
                              islandSizes.RemoveAt(auxListPointer_1);
                              islandAlphas.RemoveAt(auxListPointer_1);
                              listPointer--;
                            }
                            alphas[j2,i2] = neighbourValues[0];
                          }
                    } } }
                    break;
              } }
          } }
          return (islandAlphas[System.Array.IndexOf(islandSizes.ToArray(),islandSizes.Max())]);
        }
        void SmoothIslandBorders(float a, float b, float c, float d, int e)
        { 
          // smoothes island borders by taking the average pixel alpha value of point i,j and its neighbours weighted by 'a', 'b' and 'c' where a weights the point, 'b' weights its axis neighbours and 'c' weights its diagonal neighbours. if the average is higher than 'e' the point receives the new value of 'd' in its alpha, otherwise it receives 0 in its alpha
          for(int i=0;i<size;i++)
          {
            for(int j=0;j<size;j++)
            {
              int i_p = Math.Min (i + 1, size - 1);
              int j_p = Math.Min (j + 1, size - 1);
              int i_m = Math.Max(i-1,0);
              int j_m = Math.Max(j-1,0);
              float newValue = (a*alphas[j,i] + b*alphas[j,i_m] + b*alphas[j,i_p] + b*alphas[j_m,i] + b*alphas[j_p,i] + c*alphas[j_p,i_p] + c*alphas[j_m,i_m] + c*alphas[j_p,i_m] + c*alphas[j_m,i_p])/(a+4*b+4*c);
              if (newValue > e) { alphas[j,i] = Convert.ToInt32(d*255f); } 
              else { alphas[j,i] = 0; }
            }
          }
        }
      //         common -----------------------
        void CallDebug(string text)
        {
          if (debugging) { Console.WriteLine(updateCount.ToString("D5")+" TG"+generation_phase.ToString("D3")+"."+auxValue.ToString("D4")+"."+roundNumber.ToString("D2")+", "+text); }
        }
        void Save()
        {
          int n = 0;
          
          string path = Directory.GetCurrentDirectory()+"/Data/Saved/";
          Directory.CreateDirectory(@path);
          path += "TG.txt";
          if (!File.Exists(path)) { File.WriteAllText(path,"1"); } 
          else 
          {
            string dataRead = File.ReadAllText(path);
            n = int.Parse(dataRead);
            File.WriteAllText(path,(n+1).ToString());
          }
          Bitmap bmp = new Bitmap(size,size);
          for (int i = 0; i<size; i++) { for (int j = 0; j<size; j++) {
            Color col = Color.FromArgb(alphas[j,i],0,0,0);
            bmp.SetPixel(i,j, col);
          } }
          path = Directory.GetCurrentDirectory() + "/Data/Saved/_png/";
          Directory.CreateDirectory(@path);
          path += n.ToString("D3")+"_.png";
          bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
          bmp.Dispose();
        }
        void Showcase()
        {
          Bitmap bmp = new Bitmap(size,size);
          Bitmap bmp_to_save = new Bitmap(size,size);
          for (int i = 0; i<size; i++) { for (int j = 0; j<size; j++) {
            Color col = Color.FromArgb(alphas[j,i],0,0,0);
            bmp.SetPixel(i,j, col);
            bmp_to_save.SetPixel(i,j, col);
          }}
          form.BackgroundImage = bmp;
          form.BackgroundImageLayout = ImageLayout.Stretch;

          if (continuous_save) {
            int n = 0;
            
            string path = Directory.GetCurrentDirectory()+"/Data/Saved/";
            Directory.CreateDirectory(@path);
            path += "TG.txt";

            if (File.Exists(path)) 
            {
              string dataRead = File.ReadAllText(path);
              n = int.Parse(dataRead);
            }
            if (!_save) {
              n--;
            }
            path = Directory.GetCurrentDirectory() + "/Data/Saved/_png/"+n.ToString("D3")+"/";
            Directory.CreateDirectory(@path);
            path += updateCount.ToString("D4")+"_.png";
            bmp_to_save.Save(path, System.Drawing.Imaging.ImageFormat.Png);
          }
        }

        void finalCountdown()
        {
          if (debugging)
          {
            Console.WriteLine(updateCount.ToString("D3")+" TG"+" is going to be destroyed in 5");
            System.Threading.Thread.Sleep(1000);
            Console.WriteLine(updateCount.ToString("D3")+" TG"+" is going to be destroyed in 4");
            System.Threading.Thread.Sleep(1000);
            Console.WriteLine(updateCount.ToString("D3")+" TG"+" is going to be destroyed in 3");
            System.Threading.Thread.Sleep(1000);
            Console.WriteLine(updateCount.ToString("D3")+" TG"+" is going to be destroyed in 2");
            System.Threading.Thread.Sleep(1000);
            Console.WriteLine(updateCount.ToString("D3")+" TG"+" is going to be destroyed in 1");
            System.Threading.Thread.Sleep(1000);
            Console.WriteLine("");
          }
          // Here - destroy self;
        }
  }


// structs ------------------------------------

  public struct  Vector2Int
  {
    private int m_X;
    private int m_Y;
    public int x { get { return m_X; } set { m_X = value; } }
    public int y { get { return m_Y; } set { m_Y = value; } }

    public Vector2Int(int x, int y)
    {
        m_X = x;
        m_Y = y;
    }

    public void Set(int x, int y)
    {
        m_X = x;
        m_Y = y;
    }

    public int this[int index]
    {
      get
      {
        switch (index)
        {
          case 0: return x;
          case 1: return y;
          default:
            throw new IndexOutOfRangeException(String.Format("Invalid Vector2Int index addressed: {0}!", index));
        }
      }

      set
      {
        switch (index)
        {
          case 0: x = value; break;
          case 1: y = value; break;
          default:
            throw new IndexOutOfRangeException(String.Format("Invalid Vector2Int index addressed: {0}!", index));
        }
      }
    }
  }
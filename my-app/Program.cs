using System;
using System.Threading.Tasks;
using System.Windows.Forms;

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
            if (showcase.ToUpper() == "Y") { tg.showcase = true;  tg.form = form; }
} } } } } }
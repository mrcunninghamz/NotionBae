using Markdig.Extensions.Tables;
using Markdig.Renderers.Normalize;

namespace NotionBae.Services;
public class TableRenderer : NormalizeObjectRenderer<Table>
{

    protected override void Write(NormalizeRenderer renderer, Table table)
    {
        foreach(var block in table)
        {
            var row = (TableRow)block;
            renderer.Write("| ");
            
            for (int i = 0; i < row.Count; i++)
            {
                var cellObj = row[i];
                var cell = (TableCell)cellObj;
                renderer.Write(cell);
                renderer.Write(" | ");
            }
            
            renderer.WriteLine();

            if (row.IsHeader)
            {
                renderer.Write("| ");
                for (int i = 0; i < row.Count; i++)
                {
                    renderer.Write("---");
                    renderer.Write(" | ");
                }
                
                renderer.WriteLine();
            }
        }
        
        renderer.WriteLine();
    }
}

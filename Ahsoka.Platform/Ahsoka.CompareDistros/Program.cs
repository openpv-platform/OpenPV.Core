using Ahsoka.CompareDistros;

internal class Program
{
    private static void Main(string[] args)
    {
        DistroCompareUtility utility = new DistroCompareUtility();

        if (args.Length != 3)
        {
            Console.WriteLine("Missing Arguments: Requires <FromBSPFolder> <ToBSPFolder> <OutputFolder>");
            return;
        }

        utility.CompareDistroFolders(args[0], args[1], args[2]);
        
    }
 }
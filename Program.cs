using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using System.Xml;
using System.Xml.Linq;

using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Sites;

using Newtonsoft.Json;

namespace SC_JSONifier
{
    class Program
    {
        //we'll fill up this list with flat data during recursion and then later we'll do things
        //to the data to make it useful!
        public static List<IntermediaryItem> intermediaryItemList = new List<IntermediaryItem>();
        
        /*--------------------------------------------------------------------------------------------------*/
        //This is the target item in the tree. The "root" of the examination
        
        //home GUID (huge tree)
        public const string GUID_ROOTTARGET = "Put a GUID in here";

        /*--------------------------------------------------------------------------------------------------*/
        
        //Location to spit out the JSON data
        public const string STR_JSONFILETARGET = @"c:\SitecoreTreeData.json";

        static void Main(string[] args)
        {
            //This is unecessary and essentially insane.
            //I did this at some point, not because I should, but because I could
            //int stackSizeMod = 1073741823;

            //Threading? Why not!?
            try
            {
                ThreadStart ts = new ThreadStart(ThingWeWantToDo);
                //Thread threadToRun = new Thread(ts, stackSizeMod); //Only the cool kids do this
                Thread threadToRun = new Thread(ts);
                threadToRun.Start();               

            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }

        }

        static void ThingWeWantToDo()
        {

            Console.WriteLine("Things happening");
            //let's be an admin in Sitecore so we have the power to do things
            global::Sitecore.Security.Authentication.AuthenticationManager.SetActiveUser("sitecore\admin");
            //get web
            Database db = Factory.GetDatabase("web");
            //get our target Sitecore item to use as the root of the tree
            Item homeRoot = db.GetItem(GUID_ROOTTARGET);

            //set up a POC object and a list of that object
            SitecoreItemListItem homeListItem = new SitecoreItemListItem();
            List<SitecoreItemListItem> listOfItems = new List<SitecoreItemListItem>();

            //set the root object's Sitecore item
            homeListItem.rootItem = homeRoot;

            //Set up the first entry in the intermediary list which will be populated
            //along with the homeListItem's children during the recursive examination
            //of homeListItem's rootItem's children.
            IntermediaryItem imItem = new IntermediaryItem();
            imItem.guid = homeListItem.rootItem.ID.Guid;
            imItem.name = homeListItem.rootItem.Name;
            imItem.path = homeListItem.rootItem.Paths.Path;
            imItem.parent = homeListItem.rootItem.Parent.Name;
            imItem.parentPath = homeListItem.rootItem.Paths.ParentPath;
            imItem.parentGuid = homeListItem.rootItem.Parent.ID.Guid;
            imItem.level = homeListItem.rootItem.Axes.Level;

            //add the first entry to the globally declared intermediaryItemList
            intermediaryItemList.Add(imItem);
            
            //Here is where we recurse into the children of our ugly non-serializable POCO. This gives us an object filled with Sitecore items
            //in a tree-like child structure. Not that I really want to use anything in that item at the moment. It is a means to an end.
            //During this process, intermediaryItemList is being populated with a flat set of the data.
            homeListItem.childItems = GetChildItemList(listOfItems, homeListItem);

            Console.WriteLine("Ready to Json!");

            Guid rootItemGuid = new Guid(GUID_ROOTTARGET);

            /*----Now to take flat data and make it hierarchal!---*/
            
            //grab root object in the intermediary list and make its parent guid the same as its own guid so that
            //it's the tippity toppest object in the hierarchy otherwise the dictionary magic below doesn't work.
            IntermediaryItem firstIntermediaryItem = intermediaryItemList.Find(i => i.guid == rootItemGuid);
            firstIntermediaryItem.parentGuid = rootItemGuid;

            //Do some dictionary magics! This will take all the flat data and beat it into a more logical hierarchy that can be serialized.
            //Of course, the end result is pretty hideous, but all we care about is the root object, which we'll grab below.
            Dictionary<Guid, IntermediaryItem> dict = intermediaryItemList.ToDictionary(i => i.guid);

            foreach (IntermediaryItem jI in intermediaryItemList)
            {
                if (jI.parentGuid != jI.guid)
                {
                    IntermediaryItem parent = dict[jI.parentGuid];
                    parent.children.Add(jI);
                }
            }

            //Here we grab the top level object so we can serialize it and all its children and descendants. This object is atually
            //organized exactly like the homeListItem I made above during recursion, but THIS object can actually be serialized.
            IntermediaryItem root = dict.Values.First(i => i.parentGuid == i.guid);

            //Gimme that JSON! Human readable, please.
            //string jsonData = JsonConvert.SerializeObject(root, Newtonsoft.Json.Formatting.Indented, new Newtonsoft.Json.JsonSerializerSettings());
            
            //this is if we don't want to waste space on formatting the JSON for human readability
            string jsonData = JsonConvert.SerializeObject(root, Newtonsoft.Json.Formatting.None, new Newtonsoft.Json.JsonSerializerSettings());

            Console.WriteLine("Trying to write the file...");
            
            //Spit out the goods to disk
            try
            {
                File.WriteAllText(STR_JSONFILETARGET, jsonData);
                Console.WriteLine("Data Saved to " + STR_JSONFILETARGET);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.WriteLine("All Done! Press enter to end this horror show.");

            Console.ReadLine();

            //Proper thread completion/app termination?
            Environment.Exit(0);
        }

        //Recursive Method for digging into the tree of Sitecore items starting from the root item defined waaaaay above
        static List<SitecoreItemListItem> GetChildItemList(List<SitecoreItemListItem> listOfItems, SitecoreItemListItem topItem)
        {
            if (topItem.rootItem.HasChildren)
            {
                List<Item> childItems = topItem.rootItem.GetChildren().ToList();
                
                foreach (Item child in childItems)
                {
                    //while we recurse, we fill up a list with the traversed object in a flat manner
                    IntermediaryItem imItem = new IntermediaryItem();
                    imItem.guid = child.ID.Guid;
                    imItem.name = child.Name;
                    imItem.path = child.Paths.Path;
                    imItem.parent = child.Parent.Name;
                    imItem.parentPath = child.Paths.ParentPath;
                    imItem.parentGuid = child.Parent.ID.Guid;
                    imItem.level = child.Axes.Level;
                    //insertion of the new object into the list
                    intermediaryItemList.Add(imItem);

                    SitecoreItemListItem newChildListItem = new SitecoreItemListItem();
                    List<SitecoreItemListItem> newChildListOfChildren = new List<SitecoreItemListItem>();
                    newChildListItem.rootItem = child;
                    newChildListItem.childItems = GetChildItemList(newChildListOfChildren, newChildListItem);
                    listOfItems.Add(newChildListItem);
                }
            }
            return listOfItems;
        }
    }

    //We can get a lot of interesting data from Sitecore items and shove it into this object
    public class IntermediaryItem
    {
        public IntermediaryItem()
        {
            children = new List<IntermediaryItem>();
        }

        public Guid guid { get; set; }
        public string name { get; set; }
        public string path { get; set; }
        public string parent { get; set; }
        public string parentPath { get; set; }
        public Guid parentGuid { get; set; }
        public int level { get; set; }

        public List<IntermediaryItem> children { get; set; }
    }

    //Can't directly serialize this because Sitecore items don't inherit from serializable?
    public class SitecoreItemListItem
    {
        public Item rootItem { get; set; }
        public List<SitecoreItemListItem> childItems { get; set; }
    }
}

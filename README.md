# Sitecore_JSONifier
Create a JSON hierarchy from a tree of Sitecore items.

This code allows one to have a console application which talks to a Sitecore instance,
acquires a tree of items from that instance, and then produces a hierarchal JSON output of those items.
Once you've got a JSON file you can, for example, do fun things like feed it into something from D3 and easily produce an
interactive sitemap.

Please note: you will need to add the contents of a given Sitecore instance's web.config file to this app's app.config file.
Also, you will need to copy over the Sitecore instance's App_Config folder into this project, and then mnaually set
all the files within each folder to "Copy to Output Directory" for ease of use.

Here is a link to someone that outlined the above information in a more visual fashion
http://www.experimentsincode.com/?p=232

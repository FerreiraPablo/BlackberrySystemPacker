using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackberrySystemPacker.Helpers.Texts
{
    public static class PatchingScripts
    {
        public const string ImageCleanupScript = @"
removeapp com.twitter com.evernote com.linkedin com.tcs.maps com.rim.bb.app.facebook com.rim.bb.app.retaildemoshim sys.socialconnect.linkedin sys.socialconnect.twitter sys.socialconnect.youtube sys.socialconnect.facebook sys.cfs.box sys.cfs.dropbox sys.uri.youtube sys.weather sys.bbm sys.appworld sys.howto sys.help sys.firstlaunch sys.deviceswitch sys.paymentsystem sys.setupbuffet
replace var/pps/system/navigator/config autorun::1 autorun::0
replace var/pps/system/appconfig/sys.settings false true
replace var/pps/services/bbads/configuration www.blackberry.com/app_includes/asdk service.waitberry.com
replace var/pps/system/ota/serverurls cs.sl.blackberry.com service.waitberry.com
replace var/pps/system/ota/serverurls cp256.pushapi.na.blackberry.com service.waitberry.com
replace var/pps/system/ota/serverurls cse.dcs.blackberry.com service.waitberry.com
replace var/pps/system/ota/serverurls cse.doc.blackberry.com service.waitberry.com
touch /accounts/1000/_startup_data/sys/bbid/doneOobe  
touch /accounts/1000/sys/bbid/doneOobe  
";

    }
}

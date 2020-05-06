using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LessonBell
{
    class AllSettings
    {
        private ObservableCollection<RaspZvonkov> _rasps = new ObservableCollection<RaspZvonkov>();

        private MainSettings _mainSett;

        public AllSettings(ObservableCollection<RaspZvonkov> Rasps, MainSettings MainSett)
        {
            _rasps = Rasps;
            _mainSett = MainSett;
        }

        public void Save()
        {
            File.WriteAllText(@"settings\\config.ini", string.Empty);
            _mainSett.Save();
            
            for (int i = 0; i < _rasps.Count; i++)
            {
                _rasps[i].SaveRasp(new IniFile("settings\\config.ini"), "Rasp" + i);
            }
        }
    }
}

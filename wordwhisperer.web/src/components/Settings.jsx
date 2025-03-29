import { useState, useEffect } from 'react';
import { getSettings, updateSetting } from '../api/apiClient';

const Settings = () => {
  const [settings, setSettings] = useState({
    DefaultAccent: 'american',
    DefaultSpeed: 'normal',
    AudioQuality: 'high',
    PhoneticNotation: 'both'
  });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [saveStatus, setSaveStatus] = useState(null);
  
  useEffect(() => {
    const fetchSettings = async () => {
      try {
        setLoading(true);
        const data = await getSettings();
        
        // Convert array of settings to an object
        const settingsObj = {};
        data.forEach(item => {
          settingsObj[item.key] = item.value;
        });
        
        setSettings(settingsObj);
        setError(null);
      } catch (err) {
        console.error('Error fetching settings:', err);
        setError('Failed to load settings. Please try again later.');
      } finally {
        setLoading(false);
      }
    };
    
    fetchSettings();
  }, []);
  
  const handleSettingChange = (key, value) => {
    setSettings(prev => ({
      ...prev,
      [key]: value
    }));
  };
  
  const handleSave = async (key, value) => {
    try {
      setSaveStatus({ key, status: 'saving' });
      await updateSetting(key, value);
      setSaveStatus({ key, status: 'saved' });
      
      // Clear status after 2 seconds
      setTimeout(() => {
        setSaveStatus(prev => prev && prev.key === key ? null : prev);
      }, 2000);
    } catch (err) {
      console.error(`Error saving setting ${key}:`, err);
      setSaveStatus({ key, status: 'error' });
      
      // Clear error status after 3 seconds
      setTimeout(() => {
        setSaveStatus(prev => prev && prev.key === key ? null : prev);
      }, 3000);
    }
  };
  
  if (loading) {
    return (
      <div className="w-full max-w-3xl mx-auto p-6 text-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500 mx-auto"></div>
        <p className="mt-4 text-gray-600">Loading settings...</p>
      </div>
    );
  }
  
  if (error) {
    return (
      <div className="w-full max-w-3xl mx-auto p-6 bg-red-50 rounded-lg border border-red-200">
        <p className="text-red-600">{error}</p>
      </div>
    );
  }
  
  return (
    <div className="w-full max-w-3xl mx-auto p-6">
      <h2 className="text-2xl font-bold text-gray-800 mb-6">Settings</h2>
      
      <div className="bg-white rounded-lg shadow-md overflow-hidden">
        <div className="p-6 space-y-6">
          {/* Default Accent */}
          <div>
            <h3 className="text-lg font-medium text-gray-900 mb-2">Default Accent</h3>
            <p className="text-sm text-gray-500 mb-4">Select the default accent for word pronunciations.</p>
            
            <div className="flex gap-4">
              {['american', 'british', 'australian'].map(accent => (
                <label key={accent} className="flex items-center">
                  <input
                    type="radio"
                    name="DefaultAccent"
                    value={accent}
                    checked={settings.DefaultAccent === accent}
                    onChange={() => handleSettingChange('DefaultAccent', accent)}
                    className="h-4 w-4 text-blue-600 focus:ring-blue-500"
                  />
                  <span className="ml-2 capitalize">{accent}</span>
                </label>
              ))}
            </div>
            
            <button
              onClick={() => handleSave('DefaultAccent', settings.DefaultAccent)}
              className="mt-2 px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
            >
              {saveStatus && saveStatus.key === 'DefaultAccent' ? (
                saveStatus.status === 'saving' ? 'Saving...' : 
                saveStatus.status === 'saved' ? 'Saved!' : 
                'Error saving'
              ) : 'Save'}
            </button>
          </div>
          
          <hr />
          
          {/* Pronunciation Speed */}
          <div>
            <h3 className="text-lg font-medium text-gray-900 mb-2">Pronunciation Speed</h3>
            <p className="text-sm text-gray-500 mb-4">Choose the default speed for pronunciation playback.</p>
            
            <div className="flex gap-4">
              {['normal', 'slow'].map(speed => (
                <label key={speed} className="flex items-center">
                  <input
                    type="radio"
                    name="DefaultSpeed"
                    value={speed}
                    checked={settings.DefaultSpeed === speed}
                    onChange={() => handleSettingChange('DefaultSpeed', speed)}
                    className="h-4 w-4 text-blue-600 focus:ring-blue-500"
                  />
                  <span className="ml-2 capitalize">{speed}</span>
                </label>
              ))}
            </div>
            
            <button
              onClick={() => handleSave('DefaultSpeed', settings.DefaultSpeed)}
              className="mt-2 px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
            >
              {saveStatus && saveStatus.key === 'DefaultSpeed' ? (
                saveStatus.status === 'saving' ? 'Saving...' : 
                saveStatus.status === 'saved' ? 'Saved!' : 
                'Error saving'
              ) : 'Save'}
            </button>
          </div>
          
          <hr />
          
          {/* Audio Quality */}
          <div>
            <h3 className="text-lg font-medium text-gray-900 mb-2">Audio Quality</h3>
            <p className="text-sm text-gray-500 mb-4">Select the quality level for pronunciation audio.</p>
            
            <div className="flex gap-4">
              {['low', 'medium', 'high'].map(quality => (
                <label key={quality} className="flex items-center">
                  <input
                    type="radio"
                    name="AudioQuality"
                    value={quality}
                    checked={settings.AudioQuality === quality}
                    onChange={() => handleSettingChange('AudioQuality', quality)}
                    className="h-4 w-4 text-blue-600 focus:ring-blue-500"
                  />
                  <span className="ml-2 capitalize">{quality}</span>
                </label>
              ))}
            </div>
            
            <button
              onClick={() => handleSave('AudioQuality', settings.AudioQuality)}
              className="mt-2 px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
            >
              {saveStatus && saveStatus.key === 'AudioQuality' ? (
                saveStatus.status === 'saving' ? 'Saving...' : 
                saveStatus.status === 'saved' ? 'Saved!' : 
                'Error saving'
              ) : 'Save'}
            </button>
          </div>
          
          <hr />
          
          {/* Phonetic Notation */}
          <div>
            <h3 className="text-lg font-medium text-gray-900 mb-2">Phonetic Notation</h3>
            <p className="text-sm text-gray-500 mb-4">Choose which phonetic notation to display.</p>
            
            <div className="flex gap-4">
              {['ipa', 'simplified', 'both'].map(notation => (
                <label key={notation} className="flex items-center">
                  <input
                    type="radio"
                    name="PhoneticNotation"
                    value={notation}
                    checked={settings.PhoneticNotation === notation}
                    onChange={() => handleSettingChange('PhoneticNotation', notation)}
                    className="h-4 w-4 text-blue-600 focus:ring-blue-500"
                  />
                  <span className="ml-2 capitalize">{notation}</span>
                </label>
              ))}
            </div>
            
            <button
              onClick={() => handleSave('PhoneticNotation', settings.PhoneticNotation)}
              className="mt-2 px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
            >
              {saveStatus && saveStatus.key === 'PhoneticNotation' ? (
                saveStatus.status === 'saving' ? 'Saving...' : 
                saveStatus.status === 'saved' ? 'Saved!' : 
                'Error saving'
              ) : 'Save'}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Settings; 
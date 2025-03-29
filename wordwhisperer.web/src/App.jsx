import { useState } from 'react';
import './App.css';
import SearchBar from './components/SearchBar';
import WordResult from './components/WordResult';
import Navigation from './components/Navigation';
import History from './components/History';
import Favorites from './components/Favorites';
import Settings from './components/Settings';
import { getPronunciation, addToFavorites } from './api/apiClient';

function App() {
  const [activeTab, setActiveTab] = useState('search');
  const [searchResult, setSearchResult] = useState(null);
  const [searching, setSearching] = useState(false);
  const [error, setError] = useState(null);
  const [favoriteAdded, setFavoriteAdded] = useState(false);

  const handleSearch = async (word, accent, slow) => {
    if (!word) return;
    
    try {
      setSearching(true);
      setError(null);
      setFavoriteAdded(false);
      
      const result = await getPronunciation(word, accent, slow);
      setSearchResult(result);
      setActiveTab('search');
    } catch (err) {
      console.error('Error searching for word:', err);
      setError('Failed to fetch pronunciation. Please try again later.');
      setSearchResult(null);
    } finally {
      setSearching(false);
    }
  };

  const handleAddToFavorites = async (wordId) => {
    try {
      await addToFavorites(wordId);
      setFavoriteAdded(true);
      
      // Hide the confirmation after 3 seconds
      setTimeout(() => {
        setFavoriteAdded(false);
      }, 3000);
    } catch (err) {
      console.error('Error adding to favorites:', err);
    }
  };

  return (
    <div className="min-h-screen bg-gray-100">
      <header className="bg-white shadow-md">
        <div className="max-w-7xl mx-auto px-4 py-6 sm:px-6 lg:px-8">
          <h1 className="text-3xl font-bold text-gray-900">Word Whisperer</h1>
          <p className="text-gray-600">Pronunciation and Phonetics Assistant</p>
        </div>
      </header>

      <main>
        <Navigation activeTab={activeTab} onTabChange={setActiveTab} />
        
        {activeTab === 'search' && (
          <div className="py-4">
            <SearchBar onSearch={handleSearch} />
            
            {searching && (
              <div className="w-full max-w-3xl mx-auto p-6 text-center">
                <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500 mx-auto"></div>
                <p className="mt-4 text-gray-600">Looking up pronunciation...</p>
              </div>
            )}
            
            {error && (
              <div className="w-full max-w-3xl mx-auto p-4 bg-red-50 rounded-lg border border-red-200">
                <p className="text-red-600">{error}</p>
              </div>
            )}
            
            {favoriteAdded && (
              <div className="w-full max-w-3xl mx-auto p-4 bg-green-50 rounded-lg border border-green-200 my-2">
                <p className="text-green-600">Word added to favorites!</p>
              </div>
            )}
            
            {searchResult && <WordResult result={searchResult} addToFavorites={handleAddToFavorites} />}
          </div>
        )}
        
        {activeTab === 'history' && (
          <History onSearchWord={(word) => handleSearch(word, 'american', false)} />
        )}
        
        {activeTab === 'favorites' && (
          <Favorites onSearchWord={(word) => handleSearch(word, 'american', false)} />
        )}
        
        {activeTab === 'settings' && (
          <Settings />
        )}
      </main>
      
      <footer className="bg-white p-6 mt-10 border-t">
        <div className="max-w-7xl mx-auto text-center text-gray-500 text-sm">
          <p>© {new Date().getFullYear()} Word Whisperer. All rights reserved.</p>
        </div>
      </footer>
    </div>
  );
}

export default App;

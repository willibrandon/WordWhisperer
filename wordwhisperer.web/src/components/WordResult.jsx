import { useState } from 'react';
import { getWordAudio } from '../api/apiClient';

const WordResult = ({ result, addToFavorites }) => {
  const [isPlaying, setIsPlaying] = useState(false);
  const [showDefinition, setShowDefinition] = useState(true);
  
  if (!result) return null;
  
  const { word, accent, phonetics, definition, partOfSpeech } = result;
  const audioUrl = getWordAudio(word, accent, false);
  
  const handlePlayAudio = () => {
    setIsPlaying(true);
    const audio = new Audio(audioUrl);
    audio.onended = () => setIsPlaying(false);
    audio.play().catch(error => {
      console.error('Error playing audio:', error);
      setIsPlaying(false);
    });
  };
  
  const handleAddToFavorites = () => {
    if (addToFavorites) {
      addToFavorites(result.wordId || result.id);
    }
  };
  
  return (
    <div className="w-full max-w-3xl mx-auto p-6 bg-white rounded-lg shadow-lg my-4">
      <div className="flex items-center justify-between">
        <h2 className="text-3xl font-bold text-gray-800">{word}</h2>
        <div className="flex gap-2">
          <button
            onClick={handlePlayAudio}
            disabled={isPlaying}
            className={`p-3 rounded-full ${isPlaying ? 'bg-gray-300' : 'bg-blue-500 hover:bg-blue-600'} text-white focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2`}
            aria-label="Play pronunciation"
          >
            {isPlaying ? (
              <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <rect x="6" y="5" width="4" height="14" rx="1" />
                <rect x="14" y="5" width="4" height="14" rx="1" />
              </svg>
            ) : (
              <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z" />
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            )}
          </button>
          <button
            onClick={handleAddToFavorites}
            className="p-3 bg-yellow-500 hover:bg-yellow-600 text-white rounded-full focus:outline-none focus:ring-2 focus:ring-yellow-500 focus:ring-offset-2"
            aria-label="Add to favorites"
          >
            <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z" />
            </svg>
          </button>
        </div>
      </div>
      
      <div className="mt-4">
        <div className="flex flex-col gap-2">
          <div>
            <span className="text-sm text-gray-500">Accent: </span>
            <span className="text-gray-700 font-medium capitalize">{accent}</span>
          </div>
          
          {phonetics && (
            <>
              <div>
                <span className="text-sm text-gray-500">IPA: </span>
                <span className="text-gray-700 font-medium">{phonetics.ipa}</span>
              </div>
              <div>
                <span className="text-sm text-gray-500">Simplified: </span>
                <span className="text-gray-700 font-medium">{phonetics.simplified}</span>
              </div>
            </>
          )}
        </div>
      </div>
      
      {definition && (
        <div className="mt-4">
          <button
            className="flex items-center gap-2 text-blue-600 hover:text-blue-800 focus:outline-none"
            onClick={() => setShowDefinition(!showDefinition)}
          >
            <span>{showDefinition ? 'Hide' : 'Show'} Definition</span>
            <svg
              xmlns="http://www.w3.org/2000/svg"
              className={`h-5 w-5 transition-transform ${showDefinition ? 'rotate-180' : ''}`}
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
            </svg>
          </button>
          
          {showDefinition && (
            <div className="mt-2 p-4 bg-gray-50 rounded-lg">
              {partOfSpeech && (
                <span className="inline-block px-2 py-1 text-xs font-semibold bg-gray-200 text-gray-700 rounded mb-2">
                  {partOfSpeech}
                </span>
              )}
              <p className="text-gray-800">{definition}</p>
            </div>
          )}
        </div>
      )}
    </div>
  );
};

export default WordResult; 
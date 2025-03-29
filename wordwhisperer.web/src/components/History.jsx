import { useState, useEffect } from 'react';
import { getHistory, getWordAudio } from '../api/apiClient';

const History = ({ onSearchWord }) => {
  const [historyItems, setHistoryItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  
  useEffect(() => {
    const fetchHistory = async () => {
      try {
        setLoading(true);
        const data = await getHistory();
        setHistoryItems(data);
        setError(null);
      } catch (err) {
        console.error('Error fetching history:', err);
        setError('Failed to load history. Please try again later.');
      } finally {
        setLoading(false);
      }
    };
    
    fetchHistory();
  }, []);
  
  const handlePlayAudio = (word, accent) => {
    const audioUrl = getWordAudio(word, accent, false);
    const audio = new Audio(audioUrl);
    audio.play().catch(error => {
      console.error('Error playing audio:', error);
    });
  };
  
  const formatDate = (timestamp) => {
    const date = new Date(timestamp);
    return new Intl.DateTimeFormat('en-US', {
      year: 'numeric',
      month: 'short',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    }).format(date);
  };
  
  if (loading) {
    return (
      <div className="w-full max-w-3xl mx-auto p-6 text-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500 mx-auto"></div>
        <p className="mt-4 text-gray-600">Loading history...</p>
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
  
  if (historyItems.length === 0) {
    return (
      <div className="w-full max-w-3xl mx-auto p-6 text-center">
        <p className="text-gray-600">No history found. Start by searching for some words!</p>
      </div>
    );
  }
  
  return (
    <div className="w-full max-w-3xl mx-auto p-6">
      <h2 className="text-2xl font-bold text-gray-800 mb-4">Pronunciation History</h2>
      
      <div className="bg-white rounded-lg shadow-md overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Word
              </th>
              <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Accent
              </th>
              <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Date
              </th>
              <th scope="col" className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Actions
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {historyItems.map((item) => (
              <tr key={item.id} className="hover:bg-gray-50">
                <td className="px-6 py-4 whitespace-nowrap">
                  <button 
                    onClick={() => onSearchWord(item.word)}
                    className="text-blue-600 hover:text-blue-800 font-medium hover:underline"
                  >
                    {item.word}
                  </button>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <span className="capitalize">{item.accentUsed}</span>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                  {formatDate(item.timestamp)}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                  <button
                    onClick={() => handlePlayAudio(item.word, item.accentUsed)}
                    className="text-blue-600 hover:text-blue-900"
                    title="Play pronunciation"
                  >
                    <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15.536 8.464a5 5 0 010 7.072m2.828-9.9a9 9 0 010 12.728M5.586 15.536a5 5 0 001.414 1.414m0 0l-2.828 2.828m2.828-2.828a5 5 0 017.072 0l-2.828 2.828m0 0a9 9 0 010-12.728m0 0l2.828 2.828" />
                    </svg>
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default History; 
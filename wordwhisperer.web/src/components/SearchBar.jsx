import { useState } from 'react';

const SearchBar = ({ onSearch }) => {
  const [word, setWord] = useState('');
  const [accent, setAccent] = useState('american');
  const [slow, setSlow] = useState(false);

  const handleSubmit = (e) => {
    e.preventDefault();
    if (word.trim()) {
      onSearch(word.trim(), accent, slow);
    }
  };

  return (
    <div className="w-full max-w-3xl mx-auto p-4">
      <form onSubmit={handleSubmit} className="flex flex-col md:flex-row gap-4">
        <div className="flex-grow">
          <input
            type="text"
            value={word}
            onChange={(e) => setWord(e.target.value)}
            placeholder="Enter a word..."
            className="w-full px-4 py-2 text-lg border rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            aria-label="Word to pronounce"
          />
        </div>
        
        <div className="flex gap-4">
          <select
            value={accent}
            onChange={(e) => setAccent(e.target.value)}
            className="px-4 py-2 border rounded-lg bg-white focus:outline-none focus:ring-2 focus:ring-blue-500"
            aria-label="Select accent"
          >
            <option value="american">American</option>
            <option value="british">British</option>
            <option value="australian">Australian</option>
          </select>
          
          <label className="flex items-center gap-2 cursor-pointer">
            <input
              type="checkbox"
              checked={slow}
              onChange={(e) => setSlow(e.target.checked)}
              className="sr-only"
            />
            <span className={`relative inline-block w-10 h-6 rounded-full transition-colors ${slow ? 'bg-blue-500' : 'bg-gray-300'}`}>
              <span className={`absolute left-1 top-1 bg-white w-4 h-4 rounded-full transition-transform ${slow ? 'transform translate-x-4' : ''}`}></span>
            </span>
            <span>Slow</span>
          </label>
          
          <button
            type="submit"
            className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
          >
            Search
          </button>
        </div>
      </form>
    </div>
  );
};

export default SearchBar; 
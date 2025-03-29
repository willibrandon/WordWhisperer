import axios from 'axios';

// Create an Axios instance with default configuration
const apiClient = axios.create({
  baseURL: 'http://localhost:5104/api', // Updated to the correct port
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json'
  }
});

// Define API methods
export const getPronunciation = async (word, accent = 'american', slow = false) => {
  try {
    const response = await apiClient.get(`/pronunciation/${word}`, {
      params: { accent, slow }
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching pronunciation:', error);
    throw error;
  }
};

export const getWordAudio = (word, accent = 'american', slow = false) => {
  return `${apiClient.defaults.baseURL}/pronunciation/${word}/audio?accent=${accent}&slow=${slow}`;
};

export const getHistory = async (page = 1, pageSize = 50) => {
  try {
    const response = await apiClient.get('/history', {
      params: { page, pageSize }
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching history:', error);
    throw error;
  }
};

export const getRecentHistory = async (limit = 10) => {
  try {
    const response = await apiClient.get('/history/recent', {
      params: { limit }
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching recent history:', error);
    throw error;
  }
};

export const getFavorites = async (tag = null, page = 1, pageSize = 50) => {
  try {
    const response = await apiClient.get('/favorites', {
      params: { tag, page, pageSize }
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching favorites:', error);
    throw error;
  }
};

export const addToFavorites = async (wordId, notes = '', tags = '') => {
  try {
    const response = await apiClient.post('/favorites', {
      wordId,
      notes,
      tags
    });
    return response.data;
  } catch (error) {
    console.error('Error adding to favorites:', error);
    throw error;
  }
};

export const updateFavorite = async (id, notes, tags) => {
  try {
    const response = await apiClient.put(`/favorites/${id}`, {
      notes,
      tags
    });
    return response.data;
  } catch (error) {
    console.error('Error updating favorite:', error);
    throw error;
  }
};

export const deleteFavorite = async (id) => {
  try {
    const response = await apiClient.delete(`/favorites/${id}`);
    return response.data;
  } catch (error) {
    console.error('Error deleting favorite:', error);
    throw error;
  }
};

export const getSettings = async () => {
  try {
    const response = await apiClient.get('/settings');
    return response.data;
  } catch (error) {
    console.error('Error fetching settings:', error);
    throw error;
  }
};

export const updateSetting = async (key, value) => {
  try {
    const response = await apiClient.put(`/settings/${key}`, value);
    return response.data;
  } catch (error) {
    console.error('Error updating setting:', error);
    throw error;
  }
}; 
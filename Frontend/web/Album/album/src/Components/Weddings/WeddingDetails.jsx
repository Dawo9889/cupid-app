import { useState, useEffect } from 'react';
import axios from 'axios';

const WeddingDetails = ({ weddingId }) => {
  const [details, setDetails] = useState(null);
  const [loading, setLoading] = useState(false);
  const [qrCode, setQrCode] = useState(null);
  const [error, setError] = useState(null);

  const authData = JSON.parse(localStorage.getItem("auth"));
  const accessToken = authData?.accessToken;

  useEffect(() => {
    if (!weddingId) return;

    setLoading(true);
    setError(null);
    axios
      .get(`${import.meta.env.VITE_API_URL}/wedding/details/?id=${weddingId}`, {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      })
      .then((response) => {
        setDetails(response.data);
      })
      .catch((err) => {
        if (err.response?.status === 404) {
          setError('Wedding not found (404).');
        } else {
          setError('Error fetching wedding details.');
        }
      })
      .finally(() => {
        setLoading(false);
      });
  }, [weddingId]);

  useEffect(() => {
    if (!weddingId) return;

    setLoading(true);
    setError(null);
    axios
      .get(`${import.meta.env.VITE_API_URL}/wedding/token-qr/?id=${weddingId}`, {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
         responseType: 'arraybuffer',
      })
      .then((response) => {
        const binary = new Uint8Array(response.data); // Konwersja na tablicę binarną
        const binaryString = binary.reduce((data, byte) => data + String.fromCharCode(byte), ''); // Tworzenie ciągu znaków
        const base64QrCode = btoa(binaryString); // Konwersja na Base64
        setQrCode(base64QrCode);
      })
      .catch((err) => {
        console.log(err)
        if (err.response?.status === 404) {
          setError('Wedding not found (404).');
        } else {
          setError('Error fetching wedding details.');
        }
      })
      .finally(() => {
        setLoading(false);
      });
  }, [weddingId]);

  const DeleteWedding = (weddingId) => {
    if (!weddingId) {
      console.error('Wedding ID is required for deletion.');
      return;
    }
    axios
    .delete(`${import.meta.env.VITE_API_URL}/wedding?id=${weddingId}`, {
      headers: {
        Authorization: `Bearer ${accessToken}`,
      },
    })
    .then((response) => {
      console.log('he')
    })
    .catch((err) => {
      console.log(err)
      if (err.response?.status === 404) {
        setError('Wedding not found (404).');
      } else {
        setError('Error fetching wedding details.');
      }
    })
    .finally(() => {
      setLoading(false);
    })
  }

  return (
    <div className="h-[200px] lg:min-h-[600px] p-8 bg-project-dark-bg rounded-lg shadow-lg mt-6">
      {loading ? (
        <div className="flex items-center justify-center h-full">
          <svg
            className="animate-spin h-8 w-8 text-white"
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
          >
            <circle
              className="opacity-25"
              cx="12"
              cy="12"
              r="10"
              stroke="currentColor"
              strokeWidth="4"
            ></circle>
            <path
              className="opacity-75"
              fill="currentColor"
              d="M4 12a8 8 0 018-8V0C6.48 0 0 6.48 0 12h4zm2 5.29a8.959 8.959 0 01-2-2.29H0c.8 2.21 2.27 4.21 4 5.71v-3.42z"
            ></path>
          </svg>
        </div>
      ) : error ? (
        <p className="text-red-500 text-center">{error}</p>
      ) : details ? (
        <div>
          <h1 className="text-2xl font-bold text-white mb-4">{details.name}</h1>
          <p className="text-white">
            <strong>Date:</strong> {details.eventDate}
          </p>
          <p className="text-white">
            <strong>Description:</strong> {details.description}
          </p>
          {qrCode ? (
            <img src={`data:image/png;base64,${qrCode}`} alt="Wedding QR Code" className="w-64 h-64" />
            ) : (
            <p>No QR code available.</p>
          )}
        </div>
      ) : (
        <p className="text-gray-400 text-center">Click a wedding to see details</p>
      )}
    </div>
  );
};

export default WeddingDetails;

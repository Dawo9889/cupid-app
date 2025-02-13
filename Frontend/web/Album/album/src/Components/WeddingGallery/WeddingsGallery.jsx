import { ArrowLeftIcon } from '@heroicons/react/24/solid';
import { useEffect, useState } from 'react';
import axios from 'axios';
import WeddingPhotos from './WeddingPhotos';
import useAuth from '../hooks/useAuth';

const WeddingsGallery = () => {
  const {auth} = useAuth()

  const [weddings, setWeddings] = useState([]);
  const [selectedWedding, setSelectedWedding] = useState(null);
  
  useEffect(() => {
    axios
      .get(`${import.meta.env.VITE_API_URL}/wedding`, {
        headers: {
          Authorization: `Bearer ${auth.accessToken}`,
        },
      })
      .then((response) => {
        setWeddings(response.data);
      })
      .catch((err) => {
        console.error("Error fetching weddings:", err);
      });
  }, []);

  const handleWeddingChange = (event) => {
    const selectedId = event.target.value;
    setSelectedWedding(selectedId);
  };

  return (
    <div className='bg-project-dark'>
      <div className="flex items-center justify-between px-4">
        <a
          className="inline-flex items-center justify-center p-0.5 rounded-lg hover:bg-project-dark-bg"
          href="/admin"
        >
          <ArrowLeftIcon className="w-6 h-6 text-white sm:w-8 sm:h-8 md:w-10 md:h-10" />
        </a>

        <select
          className="mx-auto w-full max-w-xs p-2 text-white bg-project-dark-bg border border-project-blue rounded-lg focus:outline-none focus:ring-2 focus:ring-project-blue focus:border-transparent"
          value={selectedWedding || ""}
          onChange={handleWeddingChange}
        >
          <option value="" disabled>
            Select Wedding...
          </option>
          {weddings.map((wedding) => (
            <option key={wedding.id} value={wedding.id}>
              {wedding.name}
            </option>
          ))}
        </select>
      </div>
      <div className="my-4"/>
      <div className="sm:min-h-[600px] md:min-h-[500px] lg:min-h-[650px] overflow-y-auto mx-4 p-4 bg-project-dark-bg border border-project-blue rounded-lg shadow-lg">
        <div className="min-h-[650px]">
          {selectedWedding ? (
            <WeddingPhotos weddingId={selectedWedding} />
          ) : (
            <p className="text-white"></p>
          )}
        </div>
      </div>
    </div>
  );
};

export default WeddingsGallery;

import { useNavigate, Link } from "react-router-dom";
import { useContext } from "react";
import AuthContext from "../context/AuthProvider";
import logo from "../Navbar/cupidlogo.svg"
import useAuth from '../hooks/useAuth';
const Home = () => {
    // const { setAuth } = useContext(AuthContext);
    // const navigate = useNavigate();

    // const logout = async () => {
    //     setAuth({});
    //     navigate('/linkpage');
    // }
    const { auth } = useAuth();
    return (
        <section className="max-w-md mx-auto p-6 bg-white rounded-lg shadow-lg">
            <img src = {logo} alt="My Happy SVG"/>
            <button
                    className="w-full mt-4 py-2 bg-indigo-600 text-white font-semibold rounded-lg hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                >Learn More</button>
            <div>
            {/* {console.log(localStorage.getItem('auth'))} */}
            {auth?.user ? <p>Welcome, {auth.user}</p> : <p>Please log in</p>}
            </div>
        </section>
    );
}

export default Home;

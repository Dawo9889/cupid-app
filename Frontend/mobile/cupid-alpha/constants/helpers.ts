import { Alert } from "react-native";
import { removeAccessToken, removeLoggedUsername, removeRefreshToken } from "./storage";
import { router } from "expo-router";

export const formatDate = (date: Date) => {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0'); // Months are 0-indexed
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
  
    return `${year}/${month}/${day} ${hours}:${minutes}`;
  };

export const dateToString = (date: Date) => {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0'); // Months are 0-indexed
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}

export const logout = async () => {
    try {
      await removeAccessToken();
      await removeRefreshToken();
      await removeLoggedUsername();
    } catch (error: any) {
      Alert.alert('Error', error);
      return;
    }
  }
import React, { useState, useEffect } from 'react';
import './ImportDataSidebar.css';

function ImportDataSidebar({ onSelectUserStory, onClose, isSidebarVisible, selectedCredential }) {
  const [domain, setDomain] = useState(selectedCredential?.domain || '');  
  const [project, setProject] = useState('');  
  const [issueType, setIssueType] = useState('');  
  const [jiraData, setJiraData] = useState([]);  
  const [loading, setLoading] = useState(false);  
  const [error, setError] = useState(null);  
  const [username, setUsername] = useState(selectedCredential?.username || '');

  useEffect(() => {
    if (selectedCredential) {
      setDomain(selectedCredential.domain || '');
      setUsername(selectedCredential.username || '');
    }
  }, [selectedCredential]);

  const fetchJiraData = async () => {
    setLoading(true);
    setError(null);
  
    if (!selectedCredential?.id || !project || !issueType) {
      setError("Please select Jira credentials and provide Project and Issue Type.");
      setLoading(false);
      return;
    }
  
    try {
      const response = await fetch(
        'http://localhost:5104/api/UserStoryDescription/ExportAllUserStories',
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization':
              `Bearer ${sessionStorage.getItem('authToken')}`
          },
          body: JSON.stringify({
            credentialId: selectedCredential.id,
            project,
            issueType
          })
        }
      );

      if (!response.ok) {
        const errorText = await response.text();
        console.error(`Failed to fetch Jira data. Response: ${errorText}`);
        throw new Error(`Failed to fetch Jira data. Response: ${errorText}`);
      }
  
      const data = await response.json();
      setJiraData(data);
    } catch (err) {
      console.error("Error occurred while fetching Jira data:", err);
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };  

  const handleUserStoryClick = (story) => {
    onSelectUserStory(story);
  };

  const handleClearJiraData = () => {
    setJiraData([]);
    setError(null);
    //setDomain('');
    //setProject('');
    //setIssueType('');
    //setUsername('');
    //setApiToken('');
  };

  return (
    <div className={`import-sidebar ${isSidebarVisible ? 'visible' : ''}`}>
      <button className="close-btn" onClick={onClose}>X</button>
      <div className="sidebar-title"><strong>Jira User Stories</strong></div>

      <div className="input-fields">
        <p>Domain:
          <input
            type="text"
            placeholder="Domain"
            value={domain}
            readOnly
          />
        </p>

        <p>Project: <input
          type="text"
          placeholder="Project"
          value={project}
          onChange={(e) => setProject(e.target.value)}  
        /></p>

        <p>Issue Type: <input
          type="text"
          placeholder="Issue Type"
          value={issueType}
          onChange={(e) => setIssueType(e.target.value)}  
        /></p>

        <p>Username:
          <input
            type="text"
            placeholder="Username"
            value={username}
            readOnly
          />
        </p>
    
        <div className="button-container">
          <button onClick={fetchJiraData} disabled={loading} className="fetch-button">
            {loading ? 'Fetching...' : 'Fetch'}
          </button>
          <button className="clear-button" onClick={handleClearJiraData}>
            Clear
          </button>
        </div>
      </div>

      {error && <div className="error">{error}</div>}

      <div className="jira-stories">
        {jiraData && jiraData.length > 0 ? (
          jiraData.map((story, index) => (
            <div
              key={index}
              onClick={() => handleUserStoryClick(story)}
              className="jira-story-box"
            >
              <h4>{story.summary}</h4>
              <p><strong>Assignee:</strong> {story.reporter}</p>
              <p><strong>Description:</strong> {story.description}</p>
            </div>
          ))
        ) : (
          !loading && <div>No stories found.</div>
        )}
      </div>
    </div>
  );
}

export default ImportDataSidebar;